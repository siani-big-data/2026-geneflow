using GeneFlow.ApiNet2.API.Contracts.Analysis.Requests;
using GeneFlow.ApiNet2.API.Extensions;
using GeneFlow.ApiNet2.Application.Analysis.Commands.RequestAnalysis;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GeneFlow.ApiNet2.API.Endpoints.Analysis;

/// <summary>
/// Analysis endpoints for requesting trace analysis.
/// </summary>
public sealed class AnalysisEndpoints : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/traces/{traceId}/analysis")
            .WithTags("Analysis")
            .WithOpenApi()
            .RequireAuthorization();

        // Trimming
        group.MapPost("/trimming", RequestTrimming)
            .WithName("Analysis_RequestTrimming")
            .WithSummary("Request sequence trimming")
            .WithDescription("Trims low-quality ends from the sequence using the specified algorithm.")
            .Produces(StatusCodes.Status202Accepted)
            .ProducesValidationProblem()
            .Produces<ApiError>(StatusCodes.Status404NotFound);

        // Heterozygote detection
        group.MapPost("/heterozygote", RequestHeterozygoteDetection)
            .WithName("Analysis_RequestHeterozygote")
            .WithSummary("Request heterozygote detection")
            .WithDescription("Detects heterozygous positions in the chromatogram data.")
            .Produces(StatusCodes.Status202Accepted)
            .ProducesValidationProblem()
            .Produces<ApiError>(StatusCodes.Status404NotFound);

        // Motif search
        group.MapPost("/motif", RequestMotifSearch)
            .WithName("Analysis_RequestMotif")
            .WithSummary("Request motif search")
            .WithDescription("Searches for a specified pattern/motif in the sequence.")
            .Produces(StatusCodes.Status202Accepted)
            .ProducesValidationProblem()
            .Produces<ApiError>(StatusCodes.Status404NotFound);

        // Translation
        group.MapPost("/translation", RequestTranslation)
            .WithName("Analysis_RequestTranslation")
            .WithSummary("Request sequence translation")
            .WithDescription("Translates the DNA sequence to protein using the specified reading frame.")
            .Produces(StatusCodes.Status202Accepted)
            .ProducesValidationProblem()
            .Produces<ApiError>(StatusCodes.Status404NotFound);

        // ORF detection
        group.MapPost("/orf", RequestORFDetection)
            .WithName("Analysis_RequestORF")
            .WithSummary("Request ORF detection")
            .WithDescription("Detects open reading frames in the sequence.")
            .Produces(StatusCodes.Status202Accepted)
            .ProducesValidationProblem()
            .Produces<ApiError>(StatusCodes.Status404NotFound);

        // Restriction analysis
        group.MapPost("/restriction", RequestRestrictionAnalysis)
            .WithName("Analysis_RequestRestriction")
            .WithSummary("Request restriction analysis")
            .WithDescription("Analyzes restriction enzyme cut sites in the sequence.")
            .Produces(StatusCodes.Status202Accepted)
            .ProducesValidationProblem()
            .Produces<ApiError>(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> RequestTrimming(
        [FromRoute] string traceId,
        [FromBody] TrimmingRequest request,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        var options = new Dictionary<string, object>
        {
            ["algorithm"] = request.Algorithm,
            ["quality_threshold"] = request.QualityThreshold,
            ["window_size"] = request.WindowSize
        };

        var command = new RequestAnalysisCommand(traceId, AnalysisTypes.Trimming, options);
        var result = await sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? Results.Accepted()
            : result.ToHttpResult();
    }

    private static async Task<IResult> RequestHeterozygoteDetection(
        [FromRoute] string traceId,
        [FromBody] HeterozygoteRequest request,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        var options = new Dictionary<string, object>
        {
            ["secondary_peak_threshold"] = request.SecondaryPeakThreshold,
            ["min_quality"] = request.MinQuality
        };

        var command = new RequestAnalysisCommand(traceId, AnalysisTypes.Heterozygote, options);
        var result = await sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? Results.Accepted()
            : result.ToHttpResult();
    }

    private static async Task<IResult> RequestMotifSearch(
        [FromRoute] string traceId,
        [FromBody] MotifSearchRequest request,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        var options = new Dictionary<string, object>
        {
            ["pattern"] = request.Pattern,
            ["search_complement"] = request.SearchComplement,
            ["use_regex"] = request.UseRegex
        };

        var command = new RequestAnalysisCommand(traceId, AnalysisTypes.Motif, options);
        var result = await sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? Results.Accepted()
            : result.ToHttpResult();
    }

    private static async Task<IResult> RequestTranslation(
        [FromRoute] string traceId,
        [FromBody] TranslationRequest request,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        var options = new Dictionary<string, object>
        {
            ["frame"] = request.Frame,
            ["genetic_code"] = request.GeneticCode
        };

        var command = new RequestAnalysisCommand(traceId, AnalysisTypes.Translation, options);
        var result = await sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? Results.Accepted()
            : result.ToHttpResult();
    }

    private static async Task<IResult> RequestORFDetection(
        [FromRoute] string traceId,
        [FromBody] ORFDetectionRequest request,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        var options = new Dictionary<string, object>
        {
            ["min_length"] = request.MinLength,
            ["start_codons"] = request.StartCodons,
            ["stop_codons"] = request.StopCodons
        };

        var command = new RequestAnalysisCommand(traceId, AnalysisTypes.ORF, options);
        var result = await sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? Results.Accepted()
            : result.ToHttpResult();
    }

    private static async Task<IResult> RequestRestrictionAnalysis(
        [FromRoute] string traceId,
        [FromBody] RestrictionAnalysisRequest request,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        var options = new Dictionary<string, object>();
        if (!string.IsNullOrEmpty(request.Enzymes))
        {
            options["enzymes"] = request.Enzymes.Split(',').Select(e => e.Trim()).ToList();
        }

        var command = new RequestAnalysisCommand(traceId, AnalysisTypes.Restriction, options);
        var result = await sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? Results.Accepted()
            : result.ToHttpResult();
    }
}
