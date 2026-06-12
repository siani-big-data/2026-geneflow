using System.Text;
using System.Text.Json;
using GeneFlow.ApiNet2.API.Extensions;
using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Application.Traces.Queries.GetTraceById;
using GeneFlow.ApiNet2.Infrastructure.Analysis.Sse;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GeneFlow.ApiNet2.API.Endpoints.Analysis;

/// <summary>
/// Server-Sent Events endpoint that streams analysis-completion notifications
/// for a single trace to a connected browser. Used by the chromatogram view's
/// <c>AnalysisLayersPanel</c> to invalidate React-Query caches and surface
/// toast feedback the moment an analysis finishes.
/// </summary>
public sealed class AnalysisEventsEndpoints : IEndpoint
{
    /// <summary>SSE keepalive interval. Must be shorter than typical proxy idle timeouts.</summary>
    private static readonly TimeSpan KeepAliveInterval = TimeSpan.FromSeconds(20);

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/traces/{traceId}/analysis")
            .WithTags("Analysis")
            .RequireAuthorization();

        group.MapGet("/events", StreamAnalysisEvents)
            .WithName("Analysis_StreamEvents")
            .WithSummary("Stream analysis completion events for a trace (SSE)")
            .WithDescription(
                "Opens a Server-Sent Events connection that emits an event each time an " +
                "analysis (trimming, motif, translation, ORF, restriction, heterozygote) " +
                "completes or fails for the given trace. Use this in place of polling.")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task StreamAnalysisEvents(
        [FromRoute] string traceId,
        [FromServices] ICurrentUserService currentUserService,
        [FromServices] ISender sender,
        [FromServices] AnalysisEventBroker broker,
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

        // Configure SSE response headers. Setting Content-Type before any
        // write commits headers in the right order; X-Accel-Buffering disables
        // nginx buffering when the API is proxied.
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
                var outcome = evt.Success ? "success" : "failed";
                var frame = $"event: {evt.AnalysisKey}.{outcome}\ndata: {payload}\n\n";
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
            // SSE comment lines (start with ':') are ignored by EventSource
            // but keep idle proxies from closing the connection.
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
