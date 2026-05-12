using GeneFlow.ApiNet2.API.Contracts.Pipelines.Requests;
using GeneFlow.ApiNet2.API.Contracts.Pipelines.Responses;
using GeneFlow.ApiNet2.API.Extensions;
using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Application.Pipelines.Commands.AddPipelineStep;
using GeneFlow.ApiNet2.Application.Pipelines.Commands.RemovePipelineStep;
using GeneFlow.ApiNet2.Application.Pipelines.Commands.ReorderPipelineSteps;
using GeneFlow.ApiNet2.Application.Pipelines.Commands.UpdatePipelineStep;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GeneFlow.ApiNet2.API.Endpoints.Pipelines;

/// <summary>
/// Pipeline step management endpoints
/// (add/update/remove individual steps, reorder).
/// </summary>
public sealed class PipelineStepEndpoints : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/studies/{studyId}/pipelines/{pipelineId}/steps")
            .WithTags("Pipelines")
            .RequireAuthorization()
            .WithOpenApi();

        group.MapPost("/", AddPipelineStep)
            .WithName("Pipelines_AddStep")
            .WithSummary("Add a step to a pipeline")
            .WithDescription("Adds a new step to the end of the pipeline.")
            .Produces<PipelineStepResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        group.MapPut("/{stepId}", UpdatePipelineStep)
            .WithName("Pipelines_UpdateStep")
            .WithSummary("Update a pipeline step")
            .WithDescription("Updates the configuration of a pipeline step.")
            .Produces<PipelineStepResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound);

        group.MapDelete("/{stepId}", RemovePipelineStep)
            .WithName("Pipelines_RemoveStep")
            .WithSummary("Remove a step from a pipeline")
            .WithDescription("Removes a step from the pipeline.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPut("/reorder", ReorderPipelineSteps)
            .WithName("Pipelines_ReorderSteps")
            .WithSummary("Reorder pipeline steps")
            .WithDescription("Reorders the steps in a pipeline.")
            .Produces<PipelineResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem();
    }

    private static async Task<IResult> AddPipelineStep(
        [FromRoute] string studyId,
        [FromRoute] string pipelineId,
        [FromBody] AddPipelineStepRequest request,
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var result = await sender.Send(
            new AddPipelineStepCommand(
                currentUser.UserId,
                studyId,
                pipelineId,
                request.StepTypeId,
                request.Label,
                request.Configuration,
                request.IsEnabled),
            cancellationToken);

        return result.IsSuccess
            ? Results.Created(
                $"/api/v1/studies/{studyId}/pipelines/{pipelineId}/steps/{result.Value.Id}",
                result.Value.ToResponse())
            : result.Error.ToApiResult();
    }

    private static async Task<IResult> UpdatePipelineStep(
        [FromRoute] string studyId,
        [FromRoute] string pipelineId,
        [FromRoute] string stepId,
        [FromBody] UpdatePipelineStepRequest request,
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var result = await sender.Send(
            new UpdatePipelineStepCommand(
                currentUser.UserId,
                studyId,
                pipelineId,
                stepId,
                request.Label,
                request.Configuration,
                request.IsEnabled),
            cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value.ToResponse())
            : result.Error.ToApiResult();
    }

    private static async Task<IResult> RemovePipelineStep(
        [FromRoute] string studyId,
        [FromRoute] string pipelineId,
        [FromRoute] string stepId,
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var result = await sender.Send(
            new RemovePipelineStepCommand(currentUser.UserId, studyId, pipelineId, stepId),
            cancellationToken);

        return result.IsSuccess
            ? Results.NoContent()
            : result.Error.ToApiResult();
    }

    private static async Task<IResult> ReorderPipelineSteps(
        [FromRoute] string studyId,
        [FromRoute] string pipelineId,
        [FromBody] ReorderStepsRequest request,
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var result = await sender.Send(
            new ReorderPipelineStepsCommand(
                currentUser.UserId,
                studyId,
                pipelineId,
                request.StepIds),
            cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value.ToResponse())
            : result.Error.ToApiResult();
    }
}
