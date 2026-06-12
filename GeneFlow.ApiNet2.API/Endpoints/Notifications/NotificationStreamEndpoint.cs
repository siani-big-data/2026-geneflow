using System.Text;
using System.Text.Json;
using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Application.Notifications.SSE;
using Microsoft.AspNetCore.Mvc;

namespace GeneFlow.ApiNet2.API.Endpoints.Notifications;

/// <summary>
/// Server-Sent Events endpoint that streams notifications for the current
/// user. The client receives a frame each time a new notification is
/// published to the in-process <see cref="INotificationEventBroker"/>.
/// </summary>
/// <remarks>
/// Wire format mirrors <c>AnalysisEventsEndpoints</c>:
/// <code>
/// event: notification.created
/// data: { ...NotificationEventDto JSON... }
/// </code>
/// Plus a <c>: keepalive</c> comment line every 20s so proxies don't drop
/// idle connections.
/// </remarks>
public sealed class NotificationStreamEndpoint : IEndpoint
{
    private static readonly TimeSpan KeepAliveInterval = TimeSpan.FromSeconds(20);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/notifications")
            .WithTags("Notifications")
            .RequireAuthorization();

        group.MapGet("/stream", Stream)
            .WithName("Notifications_Stream")
            .WithSummary("Stream notifications (SSE)")
            .WithDescription(
                "Opens a Server-Sent Events connection that emits a frame each time a new " +
                "notification is created for the current user. Use this to update the " +
                "header badge and invalidate the notifications list cache in real time.")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);
    }

    private static async Task Stream(
        [FromServices] ICurrentUserService currentUserService,
        [FromServices] INotificationEventBroker broker,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId?.ToString();
        if (string.IsNullOrEmpty(userId))
        {
            httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var response = httpContext.Response;
        response.StatusCode = StatusCodes.Status200OK;
        response.Headers.ContentType = "text/event-stream";
        response.Headers.CacheControl = "no-cache";
        response.Headers["X-Accel-Buffering"] = "no";

        var channel = broker.Subscribe(userId);
        try
        {
            await WriteRawAsync(response, "event: ready\ndata: {}\n\n", cancellationToken);

            using var keepAliveTimer = new PeriodicTimer(KeepAliveInterval);
            var keepAliveTask = SendKeepAliveLoopAsync(response, keepAliveTimer, cancellationToken);

            await foreach (var evt in channel.Reader.ReadAllAsync(cancellationToken))
            {
                var payload = JsonSerializer.Serialize(evt, JsonOptions);
                var frame = $"event: notification.created\ndata: {payload}\n\n";
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
            broker.Unsubscribe(userId, channel);
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
