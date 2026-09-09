using GeneFlow.ApiNet2.API.Extensions;
using GeneFlow.ApiNet2.Application.Identity.Commands.FollowUser;
using GeneFlow.ApiNet2.Application.Identity.Commands.UnfollowUser;
using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Application.Identity.Queries.GetFollowers;
using GeneFlow.ApiNet2.Application.Identity.Queries.GetFollowing;
using GeneFlow.ApiNet2.Application.Identity.Queries.IsUserFollowed;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GeneFlow.ApiNet2.API.Endpoints.Identity;

/// <summary>
/// Follow-graph endpoints for the social primitive.
/// </summary>
public sealed class UserFollowEndpoints : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/users/{userId}")
            .WithTags("User Follow")
            .WithOpenApi();

        group.MapGet("/followers", GetFollowers)
            .WithName("UserFollow_GetFollowers")
            .WithSummary("List followers of a user")
            .Produces<FollowListResponse>(StatusCodes.Status200OK)
            .Produces<ApiError>(StatusCodes.Status400BadRequest);

        group.MapGet("/following", GetFollowing)
            .WithName("UserFollow_GetFollowing")
            .WithSummary("List users that a user is following")
            .Produces<FollowListResponse>(StatusCodes.Status200OK)
            .Produces<ApiError>(StatusCodes.Status400BadRequest);

        var authGroup = group.RequireAuthorization();

        authGroup.MapGet("/follow", GetIsFollowing)
            .WithName("UserFollow_IsFollowing")
            .WithSummary("Check whether current user follows the target user")
            .Produces<FollowStatusResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);

        authGroup.MapPost("/follow", FollowUser)
            .WithName("UserFollow_Follow")
            .WithSummary("Follow a user")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiError>(StatusCodes.Status400BadRequest)
            .Produces<ApiError>(StatusCodes.Status404NotFound)
            .Produces<ApiError>(StatusCodes.Status409Conflict);

        authGroup.MapDelete("/follow", UnfollowUser)
            .WithName("UserFollow_Unfollow")
            .WithSummary("Unfollow a user")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiError>(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> GetIsFollowing(
        [FromRoute] string userId,
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var query = new IsUserFollowedQuery(currentUser.UserId.ToString()!, userId);
        var result = await sender.Send(query, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.Ok(new FollowStatusResponse(result.Value));
    }

    private static async Task<IResult> FollowUser(
        [FromRoute] string userId,
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var command = new FollowUserCommand(currentUser.UserId.ToString()!, userId);
        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.NoContent();
    }

    private static async Task<IResult> UnfollowUser(
        [FromRoute] string userId,
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var command = new UnfollowUserCommand(currentUser.UserId.ToString()!, userId);
        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.NoContent();
    }

    private static async Task<IResult> GetFollowers(
        [FromRoute] string userId,
        [FromQuery] int pageNumber,
        [FromQuery] int pageSize,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        var query = new GetFollowersQuery(
            userId,
            pageNumber <= 0 ? 1 : pageNumber,
            pageSize is <= 0 or > 100 ? 20 : pageSize);

        var result = await sender.Send(query, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.Ok(new FollowListResponse(
            result.Value.Items,
            result.Value.TotalCount,
            result.Value.PageNumber,
            result.Value.PageSize));
    }

    private static async Task<IResult> GetFollowing(
        [FromRoute] string userId,
        [FromQuery] int pageNumber,
        [FromQuery] int pageSize,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        var query = new GetFollowingQuery(
            userId,
            pageNumber <= 0 ? 1 : pageNumber,
            pageSize is <= 0 or > 100 ? 20 : pageSize);

        var result = await sender.Send(query, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.Ok(new FollowListResponse(
            result.Value.Items,
            result.Value.TotalCount,
            result.Value.PageNumber,
            result.Value.PageSize));
    }
}

/// <summary>
/// Response for follow status checks.
/// </summary>
public sealed record FollowStatusResponse(bool IsFollowing);

/// <summary>
/// Response model for paginated follower / following lists.
/// </summary>
public sealed record FollowListResponse(
    IReadOnlyList<FollowUserDto> Items,
    int TotalCount,
    int PageNumber,
    int PageSize);
