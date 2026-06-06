using GeneFlow.ApiNet2.API.Contracts.Common;
using GeneFlow.ApiNet2.API.Contracts.Notifications.Requests;
using GeneFlow.ApiNet2.API.Contracts.Notifications.Responses;
using GeneFlow.ApiNet2.API.Extensions;
using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Application.Notifications.Commands.MarkAllRead;
using GeneFlow.ApiNet2.Application.Notifications.Commands.MarkNotificationRead;
using GeneFlow.ApiNet2.Application.Notifications.Commands.SetWatchLevel;
using GeneFlow.ApiNet2.Application.Notifications.Queries.GetMyNotifications;
using GeneFlow.ApiNet2.Application.Notifications.Queries.GetUnreadCount;
using GeneFlow.ApiNet2.SharedKernel.Domain.Pagination;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GeneFlow.ApiNet2.API.Endpoints.Notifications;

/// <summary>
/// REST endpoints for notifications and watch preferences. SSE streaming
/// lives in <see cref="NotificationStreamEndpoint"/>.
/// </summary>
public sealed class NotificationEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/notifications")
            .WithTags("Notifications")
            .WithOpenApi()
            .RequireAuthorization();

        group.MapGet("/", GetMyNotifications)
            .WithName("Notifications_GetMine")
            .WithSummary("List my notifications")
            .WithDescription("Returns a paginated list of notifications for the current user.")
            .Produces<PagedResponse<NotificationResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapGet("/unread-count", GetUnreadCount)
            .WithName("Notifications_UnreadCount")
            .WithSummary("Get unread count")
            .WithDescription("Returns the number of unread notifications for the current user.")
            .Produces<UnreadCountResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPost("/{notificationId}/read", MarkRead)
            .WithName("Notifications_MarkRead")
            .WithSummary("Mark a notification as read")
            .WithDescription("Marks the given notification as read. Idempotent.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiError>(StatusCodes.Status403Forbidden)
            .Produces<ApiError>(StatusCodes.Status404NotFound);

        group.MapPost("/read-all", MarkAllRead)
            .WithName("Notifications_MarkAllRead")
            .WithSummary("Mark all notifications as read")
            .WithDescription("Bulk-marks every unread notification of the current user as read.")
            .Produces<MarkAllReadResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);

        // Watch preferences hang off the study path.
        var watchGroup = app.MapGroup("/api/v1/studies/{studyId}/watch")
            .WithTags("Notifications")
            .WithOpenApi()
            .RequireAuthorization();

        watchGroup.MapPut("/", SetWatchLevel)
            .WithName("Notifications_SetWatchLevel")
            .WithSummary("Set watch level for a study")
            .WithDescription("Updates the current user's notification watch level for the given study.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiError>(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> GetMyNotifications(
        [FromQuery] bool unreadOnly,
        [FromQuery] int pageNumber,
        [FromQuery] int pageSize,
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var query = new GetMyNotificationsQuery(
            currentUser.UserId.ToString()!,
            unreadOnly,
            pageNumber > 0 ? pageNumber : 1,
            pageSize > 0 ? pageSize : PagedRequest.DefaultPageSize);

        var result = await sender.Send(query, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.Ok(result.Value.ToPagedResponse(dto => dto.ToResponse()));
    }

    private static async Task<IResult> GetUnreadCount(
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var query = new GetUnreadCountQuery(currentUser.UserId.ToString()!);
        var result = await sender.Send(query, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.Ok(new UnreadCountResponse(result.Value));
    }

    private static async Task<IResult> MarkRead(
        [FromRoute] string notificationId,
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var command = new MarkNotificationReadCommand(
            notificationId,
            currentUser.UserId.ToString()!);

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.NoContent();
    }

    private static async Task<IResult> MarkAllRead(
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var command = new MarkAllReadCommand(currentUser.UserId.ToString()!);
        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.Ok(new MarkAllReadResponse(result.Value));
    }

    private static async Task<IResult> SetWatchLevel(
        [FromRoute] string studyId,
        [FromBody] SetWatchLevelRequest request,
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var command = new SetWatchLevelCommand(
            currentUser.UserId.ToString()!,
            studyId,
            request.Level);

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.NoContent();
    }
}
