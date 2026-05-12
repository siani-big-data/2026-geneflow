using System.Text;
using System.Text.Json;
using GeneFlow.ApiNet2.API.Extensions;
using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Application.Traces.Events;
using GeneFlow.ApiNet2.Application.Traces.Queries.GetTraceById;
using GeneFlow.ApiNet2.Infrastructure.Traces.Sse;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GeneFlow.ApiNet2.API.Endpoints.Traces;

/// <summary>
/// Server-Sent Events endpoint that streams trace-processing transitions
/// (started/completed/failed) for a single trace to a connected browser.
/// Used by the trace detail page to flip Pending → Processing → Ready/Failed
/// without polling.
/// </summary>
public sealed class TraceProcessingEventsEndpoints : IEndpoint
{
    /// <summary>SSE keepalive interval. Must be shorter than typical proxy idle timeouts.</summary>
    private static readonly TimeSpan KeepAliveInterval = TimeSpan.FromSeconds(20);

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/traces/{traceId}/processing")
            .WithTags("Traces")
            .RequireAuthorization();

        group.MapGet("/events", StreamProcessingEvents)
            .WithName("Traces_StreamProcessingEvents")
            .WithSummary("Stream trace processing status events (SSE)")
            .WithDescription(
                "Opens a Server-Sent Events connection that emits an event each time " +
                "the given trace transitions between processing states (started, completed, failed).")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task StreamProcessingEvents(
        [FromRoute] string traceId,
        [FromServices] ICurrentUserService currentUserService,
        [FromServices] ISender sender,
        [FromServices] TraceProcessingEventBroker broker,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        // Trace-access guard: reuses the same query the GET /traces/{id} endpoint
        // uses, so the SSE stream honours study-membership and public-study rules.
        var accessCheck = await sender.Send(new GetTraceByIdQuery(userId, traceId), cancellationToken);
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

        var channel = broker.Subscribe(traceId);
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
            try { await keepAliveTask; } catch (OperationCanceledException) { /* expected */ }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Client disconnected — normal end of stream.
        }
        finally
        {
            broker.Unsubscribe(traceId, channel);
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
