using GeneFlow.ApiNet2.API.Contracts.Discussions.Requests;
using GeneFlow.ApiNet2.API.Contracts.Discussions.Responses;
using GeneFlow.ApiNet2.API.Extensions;
using GeneFlow.ApiNet2.Application.Discussions.Commands.CreateComment;
using GeneFlow.ApiNet2.Application.Discussions.Commands.DeleteComment;
using GeneFlow.ApiNet2.Application.Discussions.Commands.EditComment;
using GeneFlow.ApiNet2.Application.Discussions.Queries.GetDiscussion;
using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GeneFlow.ApiNet2.API.Endpoints.Discussions;

/// <summary>
/// Comment endpoints. Comments are polymorphic but for this phase only
/// Discussion parents are wired (Annotation/Trace parent types are
/// reserved by the domain enum).
/// </summary>
public sealed class CommentEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var createGroup = app.MapGroup("/api/v1/discussions/{discussionId}/comments")
            .WithTags("Comments")
            .WithOpenApi()
            .RequireAuthorization();

        createGroup.MapPost("/", CreateComment)
            .WithName("Comments_Create")
            .WithSummary("Post a comment on a discussion")
            .WithDescription("Adds a comment to the given discussion. The author must have read access to the study.")
            .Produces<CommentResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiError>(StatusCodes.Status403Forbidden)
            .Produces<ApiError>(StatusCodes.Status404NotFound);

        var editGroup = app.MapGroup("/api/v1/comments")
            .WithTags("Comments")
            .WithOpenApi()
            .RequireAuthorization();

        editGroup.MapPut("/{commentId:guid}", EditComment)
            .WithName("Comments_Edit")
            .WithSummary("Edit own comment")
            .WithDescription("Updates the markdown body of a comment. Author only.")
            .Produces<CommentResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiError>(StatusCodes.Status403Forbidden)
            .Produces<ApiError>(StatusCodes.Status404NotFound);

        editGroup.MapDelete("/{commentId:guid}", DeleteComment)
            .WithName("Comments_Delete")
            .WithSummary("Delete a comment")
            .WithDescription("Soft-deletes a comment. Author or admin only.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiError>(StatusCodes.Status403Forbidden)
            .Produces<ApiError>(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> CreateComment(
        [FromRoute] string discussionId,
        [FromBody] CreateCommentRequest request,
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var userIdString = currentUser.UserId.ToString()!;

        // The CreateComment behavior needs StudyId for membership checks;
        // resolve it from the discussion first.
        var detail = await sender.Send(new GetDiscussionQuery(discussionId, userIdString), cancellationToken);
        if (detail.IsFailure)
            return detail.ToHttpResult();

        var command = new CreateCommentCommand(
            detail.Value.StudyId,
            discussionId,
            userIdString,
            request.BodyMarkdown);

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.Created($"/api/v1/comments/{result.Value.Id}", result.Value.ToResponse());
    }

    private static async Task<IResult> EditComment(
        [FromRoute] Guid commentId,
        [FromBody] EditCommentRequest request,
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var command = new EditCommentCommand(
            commentId,
            currentUser.UserId.ToString()!,
            request.BodyMarkdown);

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.Ok(result.Value.ToResponse());
    }

    private static async Task<IResult> DeleteComment(
        [FromRoute] Guid commentId,
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var isAdmin = httpContext.User.IsInRole("Admin");

        var command = new DeleteCommentCommand(
            commentId,
            currentUser.UserId.ToString()!,
            isAdmin);

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.NoContent();
    }
}
