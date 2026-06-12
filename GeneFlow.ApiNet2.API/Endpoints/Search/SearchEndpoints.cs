using GeneFlow.ApiNet2.API.Contracts.Common;
using GeneFlow.ApiNet2.API.Contracts.Search.Responses;
using GeneFlow.ApiNet2.API.Extensions;
using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Application.Search.Commands.ReindexAll;
using GeneFlow.ApiNet2.Application.Search.Queries.ExploreFeatured;
using GeneFlow.ApiNet2.Application.Search.Queries.ExploreRecent;
using GeneFlow.ApiNet2.Application.Search.Queries.ExploreTrending;
using GeneFlow.ApiNet2.Application.Search.Queries.Feed;
using GeneFlow.ApiNet2.Application.Search.Queries.GlobalSearch;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GeneFlow.ApiNet2.API.Endpoints.Search;

/// <summary>
/// REST endpoints powering global discovery: full-text search,
/// explore (trending/recent/featured) and the personal feed.
/// </summary>
public sealed class SearchEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        // /search and /explore are public; /feed requires auth.
        var search = app.MapGroup("/api/v1/search")
            .WithTags("Search")
            .WithOpenApi();

        search.MapGet("/", GlobalSearch)
            .WithName("Search_Global")
            .WithSummary("Full-text search across the corpus")
            .WithDescription("Runs a ts_rank_cd-weighted search against title/body/tags.")
            .Produces<CursorPagedResponse<SearchHitResponse>>(StatusCodes.Status200OK)
            .Produces<ApiError>(StatusCodes.Status400BadRequest);

        var explore = app.MapGroup("/api/v1/explore")
            .WithTags("Search")
            .WithOpenApi();

        explore.MapGet("/recent", GetRecent)
            .WithName("ExploREDACTED")
            .WithSummary("Most recently updated public objects")
            .Produces<CursorPagedResponse<ExploreItemResponse>>(StatusCodes.Status200OK);

        explore.MapGet("/trending", GetTrending)
            .WithName("ExploREDACTED")
            .WithSummary("Trending public objects")
            .Produces<IReadOnlyList<ExploreItemResponse>>(StatusCodes.Status200OK);

        explore.MapGet("/featured", GetFeatured)
            .WithName("ExploREDACTED")
            .WithSummary("Curated/featured public objects (v1: trending studies)")
            .Produces<IReadOnlyList<ExploreItemResponse>>(StatusCodes.Status200OK);

        var feed = app.MapGroup("/api/v1/feed")
            .WithTags("Search")
            .WithOpenApi()
            .RequireAuthorization();

        feed.MapGet("/", GetFeed)
            .WithName("Search_Feed")
            .WithSummary("Personal feed for the current user")
            .WithDescription("Composes follows + watched studies + self activity.")
            .Produces<CursorPagedResponse<FeedItemResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);

        // One-shot backfill — requires auth. In production this should be
        // gated by an admin role; for now any authenticated user can trigger
        // it because the operation is idempotent.
        search.MapPost("/reindex", ReindexAll)
            .RequireAuthorization()
            .WithName("Search_ReindexAll")
            .WithSummary("Backfill the search index from existing studies and discussions")
            .Produces<ReindexAllResult>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);
    }

    private static async Task<IResult> ReindexAll(
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ReindexAllCommand(), cancellationToken);
        if (result.IsFailure)
            return result.ToHttpResult();
        return Results.Ok(result.Value);
    }

    private static async Task<IResult> GlobalSearch(
        [FromQuery] string q,
        [FromQuery] string? type,
        [FromQuery] string? owner,
        [FromQuery] string? tag,
        [FromQuery] string? cursor,
        [FromQuery] int pageSize,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        var query = new GlobalSearchQuery(
            q,
            type,
            owner,
            tag,
            cursor,
            pageSize > 0 ? pageSize : 20);

        var result = await sender.Send(query, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.Ok(result.Value.ToCursorResponse(dto => dto.ToResponse()));
    }

    private static async Task<IResult> GetRecent(
        [FromQuery] string? type,
        [FromQuery] string? cursor,
        [FromQuery] int pageSize,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        var query = new ExploreRecentQuery(type, cursor, pageSize > 0 ? pageSize : 20);
        var result = await sender.Send(query, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.Ok(result.Value.ToCursorResponse(dto => dto.ToResponse()));
    }

    private static async Task<IResult> GetTrending(
        [FromQuery] string? type,
        [FromQuery] int limit,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        var query = new ExploreTrendingQuery(type, limit > 0 ? limit : 20);
        var result = await sender.Send(query, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.Ok(result.Value.Select(dto => dto.ToResponse()).ToList());
    }

    private static async Task<IResult> GetFeatured(
        [FromQuery] int limit,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        var query = new ExploreFeaturedQuery(limit > 0 ? limit : 20);
        var result = await sender.Send(query, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.Ok(result.Value.Select(dto => dto.ToResponse()).ToList());
    }

    private static async Task<IResult> GetFeed(
        [FromQuery] string? cursor,
        [FromQuery] int pageSize,
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var query = new FeedQuery(
            currentUser.UserId.ToString()!,
            cursor,
            pageSize > 0 ? pageSize : 20);

        var result = await sender.Send(query, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.Ok(result.Value.ToCursorResponse(dto => dto.ToResponse()));
    }
}
