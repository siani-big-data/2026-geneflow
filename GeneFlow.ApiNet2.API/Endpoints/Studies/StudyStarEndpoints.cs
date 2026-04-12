using GeneFlow.ApiNet2.API.Contracts.Studies.Responses;
using GeneFlow.ApiNet2.API.Extensions;
using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Application.Studies.Commands.RecordStudyView;
using GeneFlow.ApiNet2.Application.Studies.Commands.StarStudy;
using GeneFlow.ApiNet2.Application.Studies.Commands.UnstarStudy;
using GeneFlow.ApiNet2.Application.Studies.Queries.GetStudyStats;
using GeneFlow.ApiNet2.Application.Studies.Queries.IsStudyStarred;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GeneFlow.ApiNet2.API.Endpoints.Studies;

/// <summary>
/// Study stars and views endpoints.
/// </summary>
public sealed class StudyStarEndpoints : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/studies/{studyId}")
            .WithTags("Study Engagement")
            .WithOpenApi();

        // Public endpoint for recording views (supports anonymous)
        group.MapPost("/views", RecordView)
            .WithName("StudyEngagement_RecordView")
            .WithSummary("Record a study view")
            .WithDescription("Records a view of the study. Supports anonymous users.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ApiError>(StatusCodes.Status404NotFound);

        // Public endpoint for getting stats
        group.MapGet("/stats", GetStudyStats)
            .WithName("StudyEngagement_GetStats")
            .WithSummary("Get study statistics")
            .WithDescription("Returns view and star counts for a study.")
            .Produces<StudyStatsResponse>(StatusCodes.Status200OK)
            .Produces<ApiError>(StatusCodes.Status404NotFound);

        // Authenticated endpoints for stars
        var authGroup = group.RequireAuthorization();

        authGroup.MapGet("/stars", GetIsStarred)
            .WithName("StudyEngagement_IsStarred")
            .WithSummary("Check if starred")
            .WithDescription("Returns whether the current user has starred this study.")
            .Produces<StarStatusResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiError>(StatusCodes.Status404NotFound);

        authGroup.MapPost("/stars", StarStudy)
            .WithName("StudyEngagement_Star")
            .WithSummary("Star a study")
            .WithDescription("Adds the study to the current user's starred list.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiError>(StatusCodes.Status404NotFound)
            .Produces<ApiError>(StatusCodes.Status409Conflict);

        authGroup.MapDelete("/stars", UnstarStudy)
            .WithName("StudyEngagement_Unstar")
            .WithSummary("Unstar a study")
            .WithDescription("Removes the study from the current user's starred list.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiError>(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> RecordView(
        [FromRoute] string studyId,
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        // Get IP address hash for deduplication
        var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var ipHash = ComputeIpHash(ipAddress);
        var userAgent = httpContext.Request.Headers.UserAgent.ToString();

        var command = new RecordStudyViewCommand(
            studyId,
            currentUser.UserId?.ToString(),
            ipHash,
            userAgent);

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.NoContent();
    }

    private static async Task<IResult> GetStudyStats(
        [FromRoute] string studyId,
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        var query = new GetStudyStatsQuery(studyId, currentUser.UserId?.ToString());
        var result = await sender.Send(query, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.Ok(result.Value.ToResponse());
    }

    private static async Task<IResult> GetIsStarred(
        [FromRoute] string studyId,
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var query = new IsStudyStarredQuery(studyId, currentUser.UserId.ToString()!);
        var result = await sender.Send(query, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.Ok(new StarStatusResponse(result.Value));
    }

    private static async Task<IResult> StarStudy(
        [FromRoute] string studyId,
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var command = new StarStudyCommand(studyId, currentUser.UserId.ToString()!);
        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.NoContent();
    }

    private static async Task<IResult> UnstarStudy(
        [FromRoute] string studyId,
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var command = new UnstarStudyCommand(studyId, currentUser.UserId.ToString()!);
        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.NoContent();
    }

    private static string ComputeIpHash(string ipAddress)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(ipAddress));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}

/// <summary>
/// Response model for star status.
/// </summary>
public sealed record StarStatusResponse(bool IsStarred);
