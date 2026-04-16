using GeneFlow.ApiNet2.API.Contracts.Traces.Requests;
using GeneFlow.ApiNet2.API.Contracts.Traces.Responses;
using GeneFlow.ApiNet2.API.Extensions;
using GeneFlow.ApiNet2.Application.Traces.Commands.CompleteTraceProcessing;
using GeneFlow.ApiNet2.Application.Traces.Commands.FailTraceProcessing;
using GeneFlow.ApiNet2.Application.Traces.Commands.StartTraceProcessing;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GeneFlow.ApiNet2.API.Endpoints.Traces;

/// <summary>
/// Trace processing endpoints (Worker API).
/// These endpoints are called by the analysis worker to update trace processing status.
/// </summary>
public sealed class TraceProcessingEndpoints : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        // Worker API endpoints - require API key authentication
        var group = app.MapGroup("/api/v1/worker/traces/{traceId}/processing")
            .WithTags("Trace Processing (Worker)")
            .WithOpenApi();
        // Note: Add worker API key authentication middleware

        group.MapPost("/start", StartProcessing)
            .WithName("Traces_StartProcessing")
            .WithSummary("Start trace processing")
            .WithDescription("Called by worker to mark trace as processing started.")
            .Produces<TraceResponse>(StatusCodes.Status200OK)
            .Produces<ApiError>(StatusCodes.Status404NotFound)
            .Produces<ApiError>(StatusCodes.Status409Conflict);

        group.MapPost("/complete", CompleteProcessing)
            .WithName("Traces_CompleteProcessing")
            .WithSummary("Complete trace processing")
            .WithDescription("Called by worker when processing is complete with quality metrics.")
            .Produces<TraceResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .Produces<ApiError>(StatusCodes.Status404NotFound)
            .Produces<ApiError>(StatusCodes.Status409Conflict);

        group.MapPost("/fail", FailProcessing)
            .WithName("Traces_FailProcessing")
            .WithSummary("Fail trace processing")
            .WithDescription("Called by worker when processing fails.")
            .Produces<TraceResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .Produces<ApiError>(StatusCodes.Status404NotFound)
            .Produces<ApiError>(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> StartProcessing(
        [FromRoute] string traceId,
        [FromServices] ISender sender = default!,
        CancellationToken cancellationToken = default)
    {
        var command = new StartTraceProcessingCommand(traceId);
        var result = await sender.Send(command, cancellationToken);

        return result.ToHttpResult();
    }

    private static async Task<IResult> CompleteProcessing(
        [FromRoute] string traceId,
        [FromBody] CompleteProcessingRequest request,
        [FromServices] ISender sender = default!,
        CancellationToken cancellationToken = default)
    {
        var command = new CompleteTraceProcessingCommand(
            traceId,
            request.AverageQualityScore,
            request.TotalBases,
            request.QualityAboveQ20Percentage,
            request.QualityAboveQ30Percentage,
            request.TrimmedLength,
            request.GcContentPercentage,
            request.HasChromatogramData);

        var result = await sender.Send(command, cancellationToken);

        return result.ToHttpResult();
    }

    private static async Task<IResult> FailProcessing(
        [FromRoute] string traceId,
        [FromBody] FailProcessingRequest request,
        [FromServices] ISender sender = default!,
        CancellationToken cancellationToken = default)
    {
        var command = new FailTraceProcessingCommand(traceId, request.Reason);
        var result = await sender.Send(command, cancellationToken);

        return result.ToHttpResult();
    }
}
