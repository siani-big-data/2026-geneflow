using GeneFlow.ApiNet2.API.Contracts.Common;
using GeneFlow.ApiNet2.API.Contracts.Pipelines.Requests;
using GeneFlow.ApiNet2.API.Contracts.Pipelines.Responses;
using GeneFlow.ApiNet2.API.Extensions;
using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Application.Pipelines.Commands.CreatePipeline;
using GeneFlow.ApiNet2.Application.Pipelines.Commands.DeletePipeline;
using GeneFlow.ApiNet2.Application.Pipelines.Commands.UpdatePipeline;
using GeneFlow.ApiNet2.Application.Pipelines.Queries.GetPipelineById;
using GeneFlow.ApiNet2.Application.Pipelines.Queries.GetStepTypes;
using GeneFlow.ApiNet2.Application.Pipelines.Queries.GetStudyPipelines;
using GeneFlow.ApiNet2.SharedKernel.Application.Common;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GeneFlow.ApiNet2.API.Endpoints.Pipelines;

/// <summary>
/// Pipeline CRUD endpoints (list, create, read, update, delete) plus
/// the read-only step-types reference catalog.
/// </summary>
public sealed class PipelineCrudEndpoints : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/pipelines/step-types", GetStepTypes)
            .WithTags("Pipelines")
            .WithName("Pipelines_GetStepTypes")
            .WithSummary("Get available step types")
            .WithDescription("Returns the list of available pipeline step types with their configuration schemas.")
            .RequireAuthorization()
            .Produces<IReadOnlyList<StepTypeResponse>>(StatusCodes.Status200OK)
            .WithOpenApi();

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
                pageNumber > 0 ? pageNumber : PagedRequestDefaults.PageNumber,
                pageSize > 0 ? pageSize : PagedRequestDefaults.PageSize,
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
}
