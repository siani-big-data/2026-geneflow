using GeneFlow.ApiNet2.API.Contracts.Common;
using GeneFlow.ApiNet2.API.Contracts.Pipelines.Responses;
using GeneFlow.ApiNet2.API.Extensions;
using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Application.Pipelines.Commands.CancelExecution;
using GeneFlow.ApiNet2.Application.Pipelines.Commands.CompleteStepExecution;
using GeneFlow.ApiNet2.Application.Pipelines.Commands.FailStepExecution;
using GeneFlow.ApiNet2.Application.Pipelines.Queries.GetExecutionById;
using GeneFlow.ApiNet2.Application.Pipelines.Queries.GetTraceExecutions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GeneFlow.ApiNet2.API.Endpoints.Pipelines;

/// <summary>
/// Pipeline execution endpoints.
/// </summary>
public sealed class PipelineExecutionEndpoints : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        // Execution endpoints (not study-scoped)
        var group = app.MapGroup("/api/v1/pipeline-executions")
            .WithTags("Pipeline Executions")
            .RequireAuthorization()
            .WithOpenApi();

        group.MapGet("/{executionId}", GetExecutionById)
            .WithName("PipelineExecutions_GetById")
            .WithSummary("Get execution by ID")
            .WithDescription("Returns the specified pipeline execution with all step details.")
            .Produces<PipelineExecutionResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/{executionId}/cancel", CancelExecution)
            .WithName("PipelineExecutions_Cancel")
            .WithSummary("Cancel an execution")
            .WithDescription("Cancels a running or pending execution.")
            .Produces<PipelineExecutionResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

        // Trace executions (within study context)
        app.MapGet("/api/v1/studies/{studyId}/traces/{traceId}/executions", GetTraceExecutions)
            .WithTags("Pipeline Executions")
            .WithName("PipelineExecutions_GetByTrace")
            .WithSummary("Get executions for a trace")
            .WithDescription("Returns a paginated list of pipeline executions for the specified trace.")
            .RequireAuthorization()
            .Produces<PagedResponse<PipelineExecutionSummaryResponse>>(StatusCodes.Status200OK)
            .WithOpenApi();

        // Worker callback endpoints (require worker API key)
        var workerGroup = app.MapGroup("/api/v1/worker/pipeline-executions")
            .WithTags("Worker Callbacks")
            .WithOpenApi();

        workerGroup.MapPost("/{executionId}/steps/{stepId}/complete", CompleteStepExecution)
            .WithName("Worker_CompleteStepExecution")
            .WithSummary("Complete a step execution")
            .WithDescription("Called by the worker to mark a step as completed.")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        workerGroup.MapPost("/{executionId}/steps/{stepId}/fail", FailStepExecution)
            .WithName("Worker_FailStepExecution")
            .WithSummary("Fail a step execution")
            .WithDescription("Called by the worker to mark a step as failed.")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> GetExecutionById(
        [FromRoute] string executionId,
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var result = await sender.Send(
            new GetExecutionByIdQuery(currentUser.UserId, executionId),
            cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value.ToResponse())
            : result.Error.ToApiResult();
    }

    private static async Task<IResult> CancelExecution(
        [FromRoute] string executionId,
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var result = await sender.Send(
            new CancelExecutionCommand(currentUser.UserId, executionId),
            cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value.ToResponse())
            : result.Error.ToApiResult();
    }

    private static async Task<IResult> GetTraceExecutions(
        [FromRoute] string studyId,
        [FromRoute] string traceId,
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
            new GetTraceExecutionsQuery(
                currentUser.UserId,
                studyId,
                traceId,
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

    private static async Task<IResult> CompleteStepExecution(
        [FromRoute] string executionId,
        [FromRoute] string stepId,
        [FromBody] CompleteStepRequest? request,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        // Note: Worker authentication should be handled by middleware/policy

        var result = await sender.Send(
            new CompleteStepExecutionCommand(
                executionId,
                stepId,
                request?.ResultSummary,
                request?.ResultData),
            cancellationToken);

        return result.IsSuccess
            ? Results.Ok()
            : result.Error.ToApiResult();
    }

    private static async Task<IResult> FailStepExecution(
        [FromRoute] string executionId,
        [FromRoute] string stepId,
        [FromBody] FailStepRequest? request,
        [FromServices] ISender sender,
        CancellationToken cancellationToken)
    {
        // Note: Worker authentication should be handled by middleware/policy

        var result = await sender.Send(
            new FailStepExecutionCommand(
                executionId,
                stepId,
                request?.ErrorMessage),
            cancellationToken);

        return result.IsSuccess
            ? Results.Ok()
            : result.Error.ToApiResult();
    }
}

/// <summary>
/// Request model for completing a step execution.
/// </summary>
public sealed record CompleteStepRequest(
    string? ResultSummary,
    string? ResultData);

/// <summary>
/// Request model for failing a step execution.
/// </summary>
public sealed record FailStepRequest(
    string? ErrorMessage);
