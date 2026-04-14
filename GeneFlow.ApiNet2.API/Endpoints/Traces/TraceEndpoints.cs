using GeneFlow.ApiNet2.API.Contracts.Traces.Requests;
using GeneFlow.ApiNet2.API.Contracts.Traces.Responses;
using GeneFlow.ApiNet2.API.Extensions;
using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Application.Traces.Commands.ArchiveTrace;
using GeneFlow.ApiNet2.Application.Traces.Commands.DeleteTrace;
using GeneFlow.ApiNet2.Application.Traces.Commands.RetryTraceProcessing;
using GeneFlow.ApiNet2.Application.Traces.Commands.UpdateTraceName;
using GeneFlow.ApiNet2.Application.Traces.Commands.UploadTrace;
using GeneFlow.ApiNet2.Application.Traces.Queries.GetStudyTraces;
using GeneFlow.ApiNet2.Application.Traces.Queries.GetTraceById;
using GeneFlow.ApiNet2.Application.Traces.Queries.GetTraceCountsByStatus;
using GeneFlow.ApiNet2.SharedKernel.Domain.Pagination;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GeneFlow.ApiNet2.API.Endpoints.Traces;

/// <summary>
/// Trace management endpoints.
/// </summary>
public sealed class TraceEndpoints : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/studies/{studyId}/traces")
            .WithTags("Traces")
            .WithOpenApi()
            .RequireAuthorization();

        group.MapGet("/", GetStudyTraces)
            .WithName("Traces_GetStudyTraces")
            .WithSummary("Get traces for a study")
            .WithDescription("Returns a paginated list of traces for a study.")
            .Produces<PagedResponse<TraceSummaryResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiError>(StatusCodes.Status404NotFound);

        group.MapGet("/counts", GetTraceCountsByStatus)
            .WithName("Traces_GetCountsByStatus")
            .WithSummary("Get trace counts by status")
            .WithDescription("Returns trace counts grouped by status for a study.")
            .Produces<TraceCountsResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiError>(StatusCodes.Status404NotFound);

        group.MapGet("/{traceId}", GetTraceById)
            .WithName("Traces_GetById")
            .WithSummary("Get trace by ID")
            .WithDescription("Returns a trace by its ID.")
            .Produces<TraceResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiError>(StatusCodes.Status404NotFound);

        group.MapPost("/upload", UploadTrace)
            .WithName("Traces_Upload")
            .WithSummary("Upload a trace file")
            .WithDescription("Uploads a new trace file for analysis.")
            .DisableAntiforgery()
            .Produces<TraceResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiError>(StatusCodes.Status404NotFound);

        group.MapPatch("/{traceId}/name", UpdateTraceName)
            .WithName("Traces_UpdateName")
            .WithSummary("Update trace name")
            .WithDescription("Updates the name of a trace.")
            .Produces<TraceResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiError>(StatusCodes.Status404NotFound);

        group.MapPost("/{traceId}/archive", ArchiveTrace)
            .WithName("Traces_Archive")
            .WithSummary("Archive a trace")
            .WithDescription("Archives a trace.")
            .Produces<TraceResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiError>(StatusCodes.Status404NotFound);

        group.MapPost("/{traceId}/retry", RetryTraceProcessing)
            .WithName("Traces_Retry")
            .WithSummary("Retry failed trace processing")
            .WithDescription("Retries processing for a failed trace.")
            .Produces<TraceResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiError>(StatusCodes.Status404NotFound);

        group.MapDelete("/{traceId}", DeleteTrace)
            .WithName("Traces_Delete")
            .WithSummary("Delete a trace")
            .WithDescription("Deletes a trace permanently.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiError>(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> GetStudyTraces(
        [FromRoute] string studyId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? searchTerm = null,
        [FromQuery] int? statusId = null,
        [FromQuery] int? formatId = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDescending = true,
        [FromServices] ISender sender = default!,
        CancellationToken cancellationToken = default)
    {
        var query = new GetStudyTracesQuery(
            studyId,
            pageNumber,
            pageSize,
            searchTerm,
            statusId,
            formatId,
            sortBy,
            sortDescending);

        var result = await sender.Send(query, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value.ToPagedResponse(t => t.ToSummaryResponse()))
            : result.Error.ToApiResult();
    }

    private static async Task<IResult> GetTraceCountsByStatus(
        [FromRoute] string studyId,
        [FromServices] ISender sender = default!,
        CancellationToken cancellationToken = default)
    {
        var query = new GetTraceCountsByStatusQuery(studyId);
        var result = await sender.Send(query, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value.ToResponse())
            : result.Error.ToApiResult();
    }

    private static async Task<IResult> GetTraceById(
        [FromRoute] string studyId,
        [FromRoute] string traceId,
        [FromServices] ISender sender = default!,
        CancellationToken cancellationToken = default)
    {
        var query = new GetTraceByIdQuery(traceId);
        var result = await sender.Send(query, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value.ToResponse())
            : result.Error.ToApiResult();
    }

    private static async Task<IResult> UploadTrace(
        [FromRoute] string studyId,
        [FromForm] string name,
        [FromForm] string? description,
        IFormFile file,
        [FromServices] ISender sender = default!,
        [FromServices] ICurrentUserService currentUserService = default!,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
            return Results.Unauthorized();

        using var stream = file.OpenReadStream();
        var fileBytes = new byte[file.Length];
        await stream.ReadExactlyAsync(fileBytes, cancellationToken);

        var command = new UploadTraceCommand(
            userId,
            studyId,
            name,
            description,
            file.FileName,
            file.ContentType,
            fileBytes);

        var result = await sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/v1/studies/{studyId}/traces/{result.Value.Id}", result.Value.ToResponse())
            : result.Error.ToApiResult();
    }

    private static async Task<IResult> UpdateTraceName(
        [FromRoute] string studyId,
        [FromRoute] string traceId,
        [FromBody] UpdateTraceNameRequest request,
        [FromServices] ISender sender = default!,
        [FromServices] ICurrentUserService currentUserService = default!,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
            return Results.Unauthorized();

        var command = new UpdateTraceNameCommand(userId, traceId, request.Name);
        var result = await sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value.ToResponse())
            : result.Error.ToApiResult();
    }

    private static async Task<IResult> ArchiveTrace(
        [FromRoute] string studyId,
        [FromRoute] string traceId,
        [FromServices] ISender sender = default!,
        [FromServices] ICurrentUserService currentUserService = default!,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
            return Results.Unauthorized();

        var command = new ArchiveTraceCommand(userId, traceId);
        var result = await sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value.ToResponse())
            : result.Error.ToApiResult();
    }

    private static async Task<IResult> RetryTraceProcessing(
        [FromRoute] string studyId,
        [FromRoute] string traceId,
        [FromServices] ISender sender = default!,
        [FromServices] ICurrentUserService currentUserService = default!,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
            return Results.Unauthorized();

        var command = new RetryTraceProcessingCommand(userId, traceId);
        var result = await sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value.ToResponse())
            : result.Error.ToApiResult();
    }

    private static async Task<IResult> DeleteTrace(
        [FromRoute] string studyId,
        [FromRoute] string traceId,
        [FromServices] ISender sender = default!,
        [FromServices] ICurrentUserService currentUserService = default!,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
            return Results.Unauthorized();

        var command = new DeleteTraceCommand(userId, traceId);
        var result = await sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? Results.NoContent()
            : result.Error.ToApiResult();
    }
}
