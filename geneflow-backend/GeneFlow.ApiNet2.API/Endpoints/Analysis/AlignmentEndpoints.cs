using GeneFlow.ApiNet2.API.Contracts.Analysis.Requests;
using GeneFlow.ApiNet2.API.Extensions;
using GeneFlow.ApiNet2.Application.Analysis.Commands.RequestAlignment;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GeneFlow.ApiNet2.API.Endpoints.Analysis;

/// <summary>
/// Alignment endpoints for sequence alignment operations.
/// </summary>
public sealed class AlignmentEndpoints : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/alignments")
            .WithTags("Alignments")
            .WithOpenApi()
            .RequireAuthorization();

        group.MapPost("/", RequestAlignment)
            .WithName("Alignments_Request")
            .WithSummary("Request sequence alignment")
            .WithDescription("Performs pairwise or multiple sequence alignment on the specified traces.")
            .Produces<AlignmentResponse>(StatusCodes.Status202Accepted)
            .ProducesValidationProblem()
            .Produces<ApiError>(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> RequestAlignment(
        [FromBody] AlignmentRequest request,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        if (request.TraceIds is null || request.TraceIds.Count < 2)
        {
            return Results.BadRequest(new ApiError(
                "Alignment.InsufficientTraces",
                "At least 2 trace IDs are required for alignment."));
        }

        var command = new RequestAlignmentCommand(
            TraceIds: request.TraceIds,
            Type: request.Type,
            BuildConsensus: request.BuildConsensus,
            ConsensusMethod: request.ConsensusMethod,
            MatchScore: request.MatchScore,
            MismatchPenalty: request.MismatchPenalty,
            GapPenalty: request.GapPenalty);

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.Accepted(
            value: new AlignmentResponse(result.Value),
            uri: $"/api/v1/alignments/{result.Value}");
    }
}

/// <summary>
/// Response containing the alignment ID.
/// </summary>
public sealed record AlignmentResponse(string AlignmentId);
