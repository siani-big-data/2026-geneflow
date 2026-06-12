using GeneFlow.ApiNet2.API.Extensions;
using GeneFlow.ApiNet2.Application.Discussions.Commands.AddReaction;
using GeneFlow.ApiNet2.Application.Discussions.Commands.RemoveReaction;
using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GeneFlow.ApiNet2.API.Endpoints.Discussions;

/// <summary>
/// Reaction endpoints. Each comment can carry many (userId, emoji) pairs
/// and the underlying unique index in the database keeps duplicates from
/// stacking.
/// </summary>
public sealed class ReactionEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/comments/{commentId:guid}/reactions")
            .WithTags("Reactions")
            .WithOpenApi()
            .RequireAuthorization();

        group.MapPost("/{emoji}", AddReaction)
            .WithName("Reactions_Add")
            .WithSummary("Add a reaction to a comment")
            .WithDescription("Records the current user's reaction to a comment. No-op if the reaction already exists.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiError>(StatusCodes.Status404NotFound);

        group.MapDelete("/{emoji}", RemoveReaction)
            .WithName("Reactions_Remove")
            .WithSummary("Remove a reaction from a comment")
            .WithDescription("Removes the current user's reaction. No-op when the reaction is missing.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiError>(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> AddReaction(
        [FromRoute] Guid commentId,
        [FromRoute] string emoji,
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var command = new AddReactionCommand(
            commentId,
            currentUser.UserId.ToString()!,
            Uri.UnescapeDataString(emoji));

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.NoContent();
    }

    private static async Task<IResult> RemoveReaction(
        [FromRoute] Guid commentId,
        [FromRoute] string emoji,
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var command = new RemoveReactionCommand(
            commentId,
            currentUser.UserId.ToString()!,
            Uri.UnescapeDataString(emoji));

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.NoContent();
    }
}
