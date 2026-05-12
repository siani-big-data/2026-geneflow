using GeneFlow.ApiNet2.API.Contracts.Common;
using GeneFlow.ApiNet2.API.Contracts.Pipelines.Requests;
using GeneFlow.ApiNet2.API.Contracts.Pipelines.Responses;
using GeneFlow.ApiNet2.API.Extensions;
using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Application.Pipelines.Commands.ActivatePipeline;
using GeneFlow.ApiNet2.Application.Pipelines.Commands.ArchivePipeline;
using GeneFlow.ApiNet2.Application.Pipelines.Commands.DeactivatePipeline;
using GeneFlow.ApiNet2.Application.Pipelines.Commands.ExecutePipeline;
using GeneFlow.ApiNet2.Application.Pipelines.Queries.GetPipelineExecutions;
using GeneFlow.ApiNet2.SharedKernel.Application.Common;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GeneFlow.ApiNet2.API.Endpoints.Pipelines;

/// <summary>
/// Pipeline lifecycle endpoints (activate / deactivate / archive),
/// execution trigger and the per-pipeline execution history listing.
/// </summary>
public sealed class PipelineActivationEndpoints : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/studies/{studyId}/pipelines/{pipelineId}")
            .WithTags("Pipelines")
            .RequireAuthorization()
            .WithOpenApi();

        group.MapPost("/activate", ActivatePipeline)
            .WithName("Pipelines_Activate")
            .WithSummary("Activate a pipeline")
            .WithDescription("Activates a draft pipeline, making it executable.")
            .Produces<PipelineResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

        group.MapPost("/deactivate", DeactivatePipeline)
            .WithName("Pipelines_Deactivate")
            .WithSummary("Deactivate a pipeline")
            .WithDescription("Deactivates an active pipeline, returning it to draft status.")
            .Produces<PipelineResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

        group.MapPost("/archive", ArchivePipeline)
            .WithName("Pipelines_Archive")
            .WithSummary("Archive a pipeline")
            .WithDescription("Archives a pipeline.")
            .Produces<PipelineResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

        group.MapPost("/execute", ExecutePipeline)
            .WithName("Pipelines_Execute")
            .WithSummary("Execute a pipeline")
            .WithDescription("Executes the pipeline on a specified trace.")
            .Produces<PipelineExecutionResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        group.MapGet("/executions", GetPipelineExecutions)
            .WithName("Pipelines_GetExecutions")
            .WithSummary("Get pipeline executions")
            .WithDescription("Returns a paginated list of executions for the specified pipeline.")
            .Produces<PagedResponse<PipelineExecutionSummaryResponse>>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> ActivatePipeline(
        [FromRoute] string studyId,
        [FromRoute] string pipelineId,
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var result = await sender.Send(
            new ActivatePipelineCommand(currentUser.UserId, studyId, pipelineId),
            cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value.ToResponse())
            : result.Error.ToApiResult();
    }

    private static async Task<IResult> DeactivatePipeline(
        [FromRoute] string studyId,
        [FromRoute] string pipelineId,
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var result = await sender.Send(
            new DeactivatePipelineCommand(currentUser.UserId, studyId, pipelineId),
            cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value.ToResponse())
            : result.Error.ToApiResult();
    }

    private static async Task<IResult> ArchivePipeline(
        [FromRoute] string studyId,
        [FromRoute] string pipelineId,
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var result = await sender.Send(
            new ArchivePipelineCommand(currentUser.UserId, studyId, pipelineId),
            cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value.ToResponse())
            : result.Error.ToApiResult();
    }

    private static async Task<IResult> ExecutePipeline(
        [FromRoute] string studyId,
        [FromRoute] string pipelineId,
        [FromBody] ExecutePipelineRequest request,
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var result = await sender.Send(
            new ExecutePipelineCommand(
                currentUser.UserId,
                studyId,
                pipelineId,
                request.TraceId),
            cancellationToken);

        return result.IsSuccess
            ? Results.Created(
                $"/api/v1/pipeline-executions/{result.Value.Id}",
                result.Value.ToResponse())
            : result.Error.ToApiResult();
    }

    private static async Task<IResult> GetPipelineExecutions(
        [FromRoute] string studyId,
        [FromRoute] string pipelineId,
        [FromQuery] int pageNumber,
        [FromQuery] int pageSize,
        [FromQuery] int? statusId,
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var result = await sender.Send(
            new GetPipelineExecutionsQuery(
                currentUser.UserId,
                studyId,
                pipelineId,
                pageNumber > 0 ? pageNumber : PagedRequestDefaults.PageNumber,
                pageSize > 0 ? pageSize : PagedRequestDefaults.PageSize,
                statusId),
            cancellationToken);

        if (result.IsFailure)
            return result.Error.ToApiResult();

        var response = new PagedResponse<PipelineExecutionSummaryResponse>(
            result.Value.Items.Select(e => e.ToSummaryResponse()).ToList(),
            result.Value.PageNumber,
            result.Value.PageSize,
            result.Value.TotalCount,
            result.Value.TotalPages,
            result.Value.HasPreviousPage,
            result.Value.HasNextPage);

        return Results.Ok(response);
    }
}
