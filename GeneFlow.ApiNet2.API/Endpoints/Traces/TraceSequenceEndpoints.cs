using GeneFlow.ApiNet2.API.Contracts.Common;
using GeneFlow.ApiNet2.API.Contracts.Traces.Responses;
using GeneFlow.ApiNet2.API.Extensions;
using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Application.Traces.Queries.GetAnalysisResult;
using GeneFlow.ApiNet2.Application.Traces.Queries.GetSequencePage;
using GeneFlow.ApiNet2.Application.Traces.Queries.GetTraceManifest;
using GeneFlow.ApiNet2.Application.Traces.Queries.ListAnalysisResults;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GeneFlow.ApiNet2.API.Endpoints.Traces;

/// <summary>
/// Endpoints for paginated trace sequence data and analysis results from datalake storage.
/// </summary>
public sealed class TraceSequenceEndpoints : IEndpoint
{
    /// <summary>
    /// Default page size (in bases) for paginated sequence reads.
    /// Sized to match the chunk granularity used by the datalake reader.
    /// </summary>
    private const int DefaultSequencePageSize = 10000;

    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/traces/{traceId}/sequence")
            .WithTags("Trace Sequence")
            .WithOpenApi()
            .RequireAuthorization();

        group.MapGet("/manifest", GetTraceManifest)
            .WithName("TraceSequence_GetManifest")
            .WithSummary("Get trace manifest")
            .WithDescription("Returns metadata about the trace's chunked sequence data, including total bases, chunk count, and available data types.")
            .Produces<TraceManifestResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiError>(StatusCodes.Status404NotFound);

        group.MapGet("/page", GetSequencePage)
            .WithName("TraceSequence_GetPage")
            .WithSummary("Get paginated sequence data")
            .WithDescription("Returns a specific page of sequence data including bases, quality scores, and chromatogram data.")
            .Produces<SequencePageResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiError>(StatusCodes.Status404NotFound);

        // Analysis results endpoints
        var analysisGroup = app.MapGroup("/api/v1/traces/{traceId}/analysis")
            .WithTags("Trace Analysis")
            .WithOpenApi()
            .RequireAuthorization();

        analysisGroup.MapGet("/", ListAnalysisResults)
            .WithName("TraceAnalysis_List")
            .WithSummary("List available analysis results")
            .WithDescription("Returns a list of all available analysis result types for this trace.")
            .Produces<AnalysisResultsListResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiError>(StatusCodes.Status404NotFound);

        analysisGroup.MapGet("/{analysisType}", GetAnalysisResult)
            .WithName("TraceAnalysis_Get")
            .WithSummary("Get analysis result")
            .WithDescription("Returns the raw JSON analysis result of the specified type.")
            .Produces<object>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiError>(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> GetTraceManifest(
        [FromRoute] string traceId,
        [FromServices] ISender sender = default!,
        [FromServices] ICurrentUserService currentUserService = default!,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
            return Results.Unauthorized();

        var query = new GetTraceManifestQuery(userId, traceId);
        var result = await sender.Send(query, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value.ToResponse())
            : result.Error.ToApiResult();
    }

    private static async Task<IResult> GetSequencePage(
        [FromRoute] string traceId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = DefaultSequencePageSize,
        [FromServices] ISender sender = default!,
        [FromServices] ICurrentUserService currentUserService = default!,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
            return Results.Unauthorized();

        var query = new GetSequencePageQuery(userId, traceId, page, pageSize);
        var result = await sender.Send(query, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value.ToResponse())
            : result.Error.ToApiResult();
    }

    private static async Task<IResult> ListAnalysisResults(
        [FromRoute] string traceId,
        [FromServices] ISender sender = default!,
        [FromServices] ICurrentUserService currentUserService = default!,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
            return Results.Unauthorized();

        var query = new ListAnalysisResultsQuery(userId, traceId);
        var result = await sender.Send(query, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(TraceMappingExtensions.ToResponse(traceId, result.Value))
            : result.Error.ToApiResult();
    }

    private static async Task<IResult> GetAnalysisResult(
        [FromRoute] string traceId,
        [FromRoute] string analysisType,
        [FromServices] ISender sender = default!,
        [FromServices] ICurrentUserService currentUserService = default!,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
            return Results.Unauthorized();

        var query = new GetAnalysisResultQuery(userId, traceId, analysisType);
        var result = await sender.Send(query, cancellationToken);

        if (!result.IsSuccess)
            return result.Error.ToApiResult();

        // Return raw JSON content
        return Results.Content(result.Value, "application/json");
    }
}
