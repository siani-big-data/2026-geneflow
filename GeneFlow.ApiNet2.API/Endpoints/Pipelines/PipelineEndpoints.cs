using GeneFlow.ApiNet2.API.Contracts.Common;
using GeneFlow.ApiNet2.API.Contracts.Pipelines.Requests;
using GeneFlow.ApiNet2.API.Contracts.Pipelines.Responses;
using GeneFlow.ApiNet2.API.Extensions;
using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Application.Pipelines.Commands.ActivatePipeline;
using GeneFlow.ApiNet2.Application.Pipelines.Commands.AddPipelineStep;
using GeneFlow.ApiNet2.Application.Pipelines.Commands.ArchivePipeline;
using GeneFlow.ApiNet2.Application.Pipelines.Commands.CreatePipeline;
using GeneFlow.ApiNet2.Application.Pipelines.Commands.DeactivatePipeline;
using GeneFlow.ApiNet2.Application.Pipelines.Commands.DeletePipeline;
using GeneFlow.ApiNet2.Application.Pipelines.Commands.ExecutePipeline;
using GeneFlow.ApiNet2.Application.Pipelines.Commands.RemovePipelineStep;
using GeneFlow.ApiNet2.Application.Pipelines.Commands.ReorderPipelineSteps;
using GeneFlow.ApiNet2.Application.Pipelines.Commands.UpdatePipeline;
using GeneFlow.ApiNet2.Application.Pipelines.Commands.UpdatePipelineStep;
using GeneFlow.ApiNet2.Application.Pipelines.Queries.GetPipelineById;
using GeneFlow.ApiNet2.Application.Pipelines.Queries.GetPipelineExecutions;
using GeneFlow.ApiNet2.Application.Pipelines.Queries.GetStepTypes;
using GeneFlow.ApiNet2.Application.Pipelines.Queries.GetStudyPipelines;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GeneFlow.ApiNet2.API.Endpoints.Pipelines;

/// <summary>
/// Pipeline management endpoints.
/// </summary>
public sealed class PipelineEndpoints : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        // Step types (public reference data)
        app.MapGet("/api/v1/pipelines/step-types", GetStepTypes)
            .WithTags("Pipelines")
            .WithName("Pipelines_GetStepTypes")
            .WithSummary("Get available step types")
            .WithDescription("Returns the list of available pipeline step types with their configuration schemas.")
            .RequireAuthorization()
            .Produces<IReadOnlyList<StepTypeResponse>>(StatusCodes.Status200OK)
            .WithOpenApi();

        // Study-scoped pipeline endpoints
        var group = app.MapGroup("/api/v1/studies/{studyId}/pipelines")
            .WithTags("Pipelines")
            .RequireAuthorization()
            .WithOpenApi();

        group.MapGet("/", GetStudyPipelines)
            .WithName("Pipelines_GetByStudy")
            .WithSummary("Get pipelines for a study")
            .WithDescription("Returns a paginated list of pipelines for the specified study.")
            .Produces<PagedResponse<PipelineSummaryResponse>>(StatusCodes.Status200OK);

        group.MapPost("/", CreatePipeline)
            .WithName("Pipelines_Create")
            .WithSummary("Create a new pipeline")
            .WithDescription("Creates a new pipeline in the specified study.")
            .Produces<PipelineResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        group.MapGet("/{pipelineId}", GetPipelineById)
            .WithName("Pipelines_GetById")
            .WithSummary("Get a pipeline by ID")
            .WithDescription("Returns the specified pipeline with all its steps.")
            .Produces<PipelineResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPut("/{pipelineId}", UpdatePipeline)
            .WithName("Pipelines_Update")
            .WithSummary("Update a pipeline")
            .WithDescription("Updates the name and description of a pipeline.")
            .Produces<PipelineResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound);

        group.MapDelete("/{pipelineId}", DeletePipeline)
            .WithName("Pipelines_Delete")
            .WithSummary("Delete a pipeline")
            .WithDescription("Deletes the specified pipeline.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        // Status management
        group.MapPost("/{pipelineId}/activate", ActivatePipeline)
            .WithName("Pipelines_Activate")
            .WithSummary("Activate a pipeline")
            .WithDescription("Activates a draft pipeline, making it executable.")
            .Produces<PipelineResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

        group.MapPost("/{pipelineId}/deactivate", DeactivatePipeline)
            .WithName("Pipelines_Deactivate")
            .WithSummary("Deactivate a pipeline")
            .WithDescription("Deactivates an active pipeline, returning it to draft status.")
            .Produces<PipelineResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

        group.MapPost("/{pipelineId}/archive", ArchivePipeline)
            .WithName("Pipelines_Archive")
            .WithSummary("Archive a pipeline")
            .WithDescription("Archives a pipeline.")
            .Produces<PipelineResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

        // Step management
        group.MapPost("/{pipelineId}/steps", AddPipelineStep)
            .WithName("Pipelines_AddStep")
            .WithSummary("Add a step to a pipeline")
            .WithDescription("Adds a new step to the end of the pipeline.")
            .Produces<PipelineStepResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        group.MapPut("/{pipelineId}/steps/{stepId}", UpdatePipelineStep)
            .WithName("Pipelines_UpdateStep")
            .WithSummary("Update a pipeline step")
            .WithDescription("Updates the configuration of a pipeline step.")
            .Produces<PipelineStepResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound);

        group.MapDelete("/{pipelineId}/steps/{stepId}", RemovePipelineStep)
            .WithName("Pipelines_RemoveStep")
            .WithSummary("Remove a step from a pipeline")
            .WithDescription("Removes a step from the pipeline.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPut("/{pipelineId}/steps/reorder", ReorderPipelineSteps)
            .WithName("Pipelines_ReorderSteps")
            .WithSummary("Reorder pipeline steps")
            .WithDescription("Reorders the steps in a pipeline.")
            .Produces<PipelineResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem();

        // Execution
        group.MapPost("/{pipelineId}/execute", ExecutePipeline)
            .WithName("Pipelines_Execute")
            .WithSummary("Execute a pipeline")
            .WithDescription("Executes the pipeline on a specified trace.")
            .Produces<PipelineExecutionResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        group.MapGet("/{pipelineId}/executions", GetPipelineExecutions)
            .WithName("Pipelines_GetExecutions")
            .WithSummary("Get pipeline executions")
            .WithDescription("Returns a paginated list of executions for the specified pipeline.")
            .Produces<PagedResponse<PipelineExecutionSummaryResponse>>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> GetStepTypes(
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var result = await sender.Send(
            new GetStepTypesQuery(currentUser.UserId),
            cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value.Select(s => s.ToResponse()).ToList())
            : result.Error.ToApiResult();
    }

    private static async Task<IResult> GetStudyPipelines(
        [FromRoute] string studyId,
        [FromQuery] int pageNumber,
        [FromQuery] int pageSize,
        [FromQuery] string? searchTerm,
        [FromQuery] int? statusId,
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var result = await sender.Send(
            new GetStudyPipelinesQuery(
                currentUser.UserId,
                studyId,
                pageNumber > 0 ? pageNumber : 1,
                pageSize > 0 ? pageSize : 20,
                searchTerm,
                statusId),
            cancellationToken);

        if (result.IsFailure)
            return result.Error.ToApiResult();

        var response = new PagedResponse<PipelineSummaryResponse>(
            result.Value.Items.Select(p => p.ToSummaryResponse()).ToList(),
            result.Value.PageNumber,
            result.Value.PageSize,
            result.Value.TotalCount,
            result.Value.TotalPages,
            result.Value.HasPreviousPage,
            result.Value.HasNextPage);

        return Results.Ok(response);
    }

    private static async Task<IResult> CreatePipeline(
        [FromRoute] string studyId,
        [FromBody] CreatePipelineRequest request,
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var result = await sender.Send(
            new CreatePipelineCommand(
                currentUser.UserId,
                studyId,
                request.Name,
                request.Description),
            cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/v1/studies/{studyId}/pipelines/{result.Value.Id}", result.Value.ToResponse())
            : result.Error.ToApiResult();
    }

    private static async Task<IResult> GetPipelineById(
        [FromRoute] string studyId,
        [FromRoute] string pipelineId,
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var result = await sender.Send(
            new GetPipelineByIdQuery(currentUser.UserId, studyId, pipelineId),
            cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value.ToResponse())
            : result.Error.ToApiResult();
    }

    private static async Task<IResult> UpdatePipeline(
        [FromRoute] string studyId,
        [FromRoute] string pipelineId,
        [FromBody] UpdatePipelineRequest request,
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var result = await sender.Send(
            new UpdatePipelineCommand(
                currentUser.UserId,
                studyId,
                pipelineId,
                request.Name,
                request.Description),
            cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value.ToResponse())
            : result.Error.ToApiResult();
    }

    private static async Task<IResult> DeletePipeline(
        [FromRoute] string studyId,
        [FromRoute] string pipelineId,
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var result = await sender.Send(
            new DeletePipelineCommand(currentUser.UserId, studyId, pipelineId),
            cancellationToken);

        return result.IsSuccess
            ? Results.NoContent()
            : result.Error.ToApiResult();
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
                pageNumber > 0 ? pageNumber : 1,
                pageSize > 0 ? pageSize : 20,
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
