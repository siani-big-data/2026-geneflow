using System.Text;
using System.Text.Json;
using GeneFlow.ApiNet2.API.Extensions;
using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Application.Pipelines.Queries.GetExecutionById;
using GeneFlow.ApiNet2.Infrastructure.Pipelines.Sse;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GeneFlow.ApiNet2.API.Endpoints.Pipelines;

/// <summary>
/// Server-Sent Events endpoint that streams pipeline-execution progress
/// (started / step.completed / completed / failed) to a connected browser.
/// Used by the executions UI to advance the step counter without polling.
/// </summary>
public sealed class PipelineExecutionEventsEndpoints : IEndpoint
{
    /// <summary>SSE keepalive interval. Must be shorter than typical proxy idle timeouts.</summary>
    private static readonly TimeSpan KeepAliveInterval = TimeSpan.FromSeconds(20);

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/pipeline-executions/{executionId}")
            .WithTags("PipelineExecutions")
            .RequireAuthorization();

        group.MapGet("/events", StreamExecutionEvents)
            .WithName("PipelineExecutions_StreamEvents")
            .WithSummary("Stream pipeline execution progress events (SSE)")
            .WithDescription(
                "Opens a Server-Sent Events connection that emits an event each time the " +
                "pipeline execution transitions state (started, each step completed, completed, failed).")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task StreamExecutionEvents(
        [FromRoute] string executionId,
        [FromServices] ICurrentUserService currentUserService,
        [FromServices] ISender sender,
        [FromServices] PipelineExecutionEventBroker broker,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        // Access guard: reuses the existing GetExecutionById query so the SSE
        // stream honours the same study-membership / public-study rules.
        var accessCheck = await sender.Send(new GetExecutionByIdQuery(userId, executionId), cancellationToken);
        if (accessCheck.IsFailure)
        {
            await accessCheck.Error.ToApiResult().ExecuteAsync(httpContext);
            return;
        }

        var response = httpContext.Response;
        response.StatusCode = StatusCodes.Status200OK;
        response.Headers.ContentType = "text/event-stream";
        response.Headers.CacheControl = "no-cache";
        response.Headers["X-Accel-Buffering"] = "no";

        var channel = broker.Subscribe(executionId);
        try
        {
            await WriteRawAsync(response, "event: ready\ndata: {}\n\n", cancellationToken);

            using var keepAliveTimer = new PeriodicTimer(KeepAliveInterval);
            var keepAliveTask = SendKeepAliveLoopAsync(response, keepAliveTimer, cancellationToken);

            await foreach (var evt in channel.Reader.ReadAllAsync(cancellationToken))
            {
                var payload = JsonSerializer.Serialize(evt, JsonOptions);
                var frame = $"event: {evt.EventType}\ndata: {payload}\n\n";
                await WriteRawAsync(response, frame, cancellationToken);
            }

            keepAliveTimer.Dispose();
            try
            { await keepAliveTask; }
            catch (OperationCanceledException) { /* expected */ }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Client disconnected — normal end of stream.
        }
        finally
        {
            broker.Unsubscribe(executionId, channel);
        }
    }

    private static async Task SendKeepAliveLoopAsync(
        HttpResponse response,
        PeriodicTimer timer,
        CancellationToken cancellationToken)
    {
        while (await timer.WaitForNextTickAsync(cancellationToken))
        {
            await WriteRawAsync(response, ": keepalive\n\n", cancellationToken);
        }
    }

    private static async Task WriteRawAsync(HttpResponse response, string frame, CancellationToken cancellationToken)
    {
        var bytes = Encoding.UTF8.GetBytes(frame);
        await response.Body.WriteAsync(bytes, cancellationToken);
        await response.Body.FlushAsync(cancellationToken);
    }
}
