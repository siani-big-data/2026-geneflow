using GeneFlow.ApiNet2.API.Contracts.Traces.Requests;
using GeneFlow.ApiNet2.API.Contracts.Traces.Responses;
using GeneFlow.ApiNet2.API.Extensions;
using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Application.Traces.Commands.AutoTrimTrace;
using GeneFlow.ApiNet2.Application.Traces.Commands.CreateSequenceEdit;
using GeneFlow.ApiNet2.Application.Traces.Commands.ManualTrimTrace;
using GeneFlow.ApiNet2.Application.Traces.Commands.UndoAllSequenceEdits;
using GeneFlow.ApiNet2.Application.Traces.Commands.UndoSequenceEdit;
using GeneFlow.ApiNet2.Application.Traces.Commands.UndoTrimTrace;
using GeneFlow.ApiNet2.Application.Traces.Queries.GetEditedSequence;
using GeneFlow.ApiNet2.Application.Traces.Queries.GetReverseComplement;
using GeneFlow.ApiNet2.Application.Traces.Queries.GetSequenceEdits;
using GeneFlow.ApiNet2.Application.Traces.Queries.GetTraceTrims;
using GeneFlow.ApiNet2.Application.Traces.Queries.GetTrimmedSequence;
using GeneFlow.ApiNet2.Application.Traces.Queries.PreviewTrim;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GeneFlow.ApiNet2.API.Endpoints.Traces;

/// <summary>
/// Trace editing endpoints (trim, sequence edits, reverse complement).
/// </summary>
public sealed class TraceEditingEndpoints : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/studies/{studyId}/traces/{traceId}")
            .WithTags("Trace Editing")
            .WithOpenApi()
            .RequireAuthorization();

        // Trimming endpoints (multiple trims per trace)
        group.MapGet("/trims", GetTrims)
            .WithName("Traces_GetTrims")
            .WithSummary("Get all trims")
            .WithDescription("Returns all trim operations for a trace.")
            .Produces<IReadOnlyList<TraceTrimResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiError>(StatusCodes.Status404NotFound);

        group.MapPost("/trims", AddTrim)
            .WithName("Traces_AddTrim")
            .WithSummary("Add trim")
            .WithDescription("Adds a new trim operation to a trace. Multiple trims can be added.")
            .Produces<TraceTrimResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiError>(StatusCodes.Status404NotFound);

        group.MapPost("/trims/auto", AutoTrimTrace)
            .WithName("Traces_AutoTrim")
            .WithSummary("Auto-trim trace")
            .WithDescription("Automatically trims a trace using quality-based algorithm. Creates separate trim operations for 5' and 3' ends.")
            .Produces<IReadOnlyList<TraceTrimResponse>>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiError>(StatusCodes.Status404NotFound);

        group.MapPost("/trims/preview", PreviewTrim)
            .WithName("Traces_PreviewTrim")
            .WithSummary("Preview auto-trim")
            .WithDescription("Previews auto-trim results without applying them.")
            .Produces<TrimPreviewResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiError>(StatusCodes.Status404NotFound);

        group.MapDelete("/trims/{trimId}", UndoTrim)
            .WithName("Traces_UndoTrim")
            .WithSummary("Undo specific trim")
            .WithDescription("Undoes a specific trim operation.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiError>(StatusCodes.Status404NotFound);

        group.MapDelete("/trims", UndoAllTrims)
            .WithName("Traces_UndoAllTrims")
            .WithSummary("Undo all trims")
            .WithDescription("Undoes all active trim operations on a trace.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiError>(StatusCodes.Status404NotFound);

        group.MapGet("/sequence/trimmed", GetTrimmedSequence)
            .WithName("Traces_GetTrimmedSequence")
            .WithSummary("Get trimmed sequence")
            .WithDescription("Returns the sequence with all active trims applied.")
            .Produces<TrimmedSequenceResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiError>(StatusCodes.Status404NotFound);

        // Sequence editing endpoints
        group.MapPost("/edits", CreateSequenceEdit)
            .WithName("Traces_CreateEdit")
            .WithSummary("Create sequence edit")
            .WithDescription("Creates a new sequence edit on a trace.")
            .Produces<SequenceEditResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiError>(StatusCodes.Status404NotFound);

        group.MapGet("/edits", GetSequenceEdits)
            .WithName("Traces_GetEdits")
            .WithSummary("Get sequence edits")
            .WithDescription("Returns all sequence edits for a trace.")
            .Produces<IReadOnlyList<SequenceEditResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiError>(StatusCodes.Status404NotFound);

        group.MapDelete("/edits/{editId}", UndoSequenceEdit)
            .WithName("Traces_UndoEdit")
            .WithSummary("Undo sequence edit")
            .WithDescription("Undoes a specific sequence edit.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiError>(StatusCodes.Status404NotFound);

        group.MapDelete("/edits", UndoAllSequenceEdits)
            .WithName("Traces_UndoAllEdits")
            .WithSummary("Undo all sequence edits")
            .WithDescription("Undoes all active sequence edits on a trace.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiError>(StatusCodes.Status404NotFound);

        group.MapGet("/sequence/edited", GetEditedSequence)
            .WithName("Traces_GetEditedSequence")
            .WithSummary("Get edited sequence")
            .WithDescription("Returns the sequence with all edits applied.")
            .Produces<EditedSequenceResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiError>(StatusCodes.Status404NotFound);

        // Reverse complement
        group.MapGet("/reverse-complement", GetReverseComplement)
            .WithName("Traces_GetReverseComplement")
            .WithSummary("Get reverse complement")
            .WithDescription("Returns the reverse complement of the trace sequence.")
            .Produces<ReverseComplementResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiError>(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> GetTrims(
        [FromRoute] string studyId,
        [FromRoute] string traceId,
        [FromQuery] bool activeOnly = true,
        [FromServices] ISender sender = default!,
        [FromServices] ICurrentUserService currentUserService = default!,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
            return Results.Unauthorized();

        var query = new GetTraceTrimsQuery(userId, traceId, activeOnly);
        var result = await sender.Send(query, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value.ToResponses())
            : result.Error.ToApiResult();
    }

    private static async Task<IResult> AddTrim(
        [FromRoute] string studyId,
        [FromRoute] string traceId,
        [FromBody] AddTrimRequest request,
        [FromServices] ISender sender = default!,
        [FromServices] ICurrentUserService currentUserService = default!,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
            return Results.Unauthorized();

        var command = new ManualTrimTraceCommand(
            userId,
            traceId,
            request.StartPosition,
            request.EndPosition,
            request.TrimEnd,
            request.Reason);

        var result = await sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/v1/studies/{studyId}/traces/{traceId}/trims/{result.Value.Id}", result.Value.ToResponse())
            : result.Error.ToApiResult();
    }

    private static async Task<IResult> AutoTrimTrace(
        [FromRoute] string studyId,
        [FromRoute] string traceId,
        [FromBody] AutoTrimRequest request,
        [FromServices] ISender sender = default!,
        [FromServices] ICurrentUserService currentUserService = default!,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
            return Results.Unauthorized();

        var command = new AutoTrimTraceCommand(
            userId,
            traceId,
            request.QualityThreshold,
            request.WindowSize,
            request.MinimumLength);

        var result = await sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value.ToResponses())
            : result.Error.ToApiResult();
    }

    private static async Task<IResult> PreviewTrim(
        [FromRoute] string studyId,
        [FromRoute] string traceId,
        [FromBody] AutoTrimRequest request,
        [FromServices] ISender sender = default!,
        CancellationToken cancellationToken = default)
    {
        var query = new PreviewTrimQuery(
            traceId,
            request.QualityThreshold,
            request.WindowSize,
            request.MinimumLength);

        var result = await sender.Send(query, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value.ToResponse())
            : result.Error.ToApiResult();
    }

    private static async Task<IResult> UndoTrim(
        [FromRoute] string studyId,
        [FromRoute] string traceId,
        [FromRoute] string trimId,
        [FromServices] ISender sender = default!,
        [FromServices] ICurrentUserService currentUserService = default!,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
            return Results.Unauthorized();

        var command = new UndoTrimTraceCommand(userId, traceId, trimId);
        var result = await sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? Results.NoContent()
            : result.Error.ToApiResult();
    }

    private static async Task<IResult> UndoAllTrims(
        [FromRoute] string studyId,
        [FromRoute] string traceId,
        [FromServices] ISender sender = default!,
        [FromServices] ICurrentUserService currentUserService = default!,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
            return Results.Unauthorized();

        var command = new UndoTrimTraceCommand(userId, traceId, UndoAll: true);
        var result = await sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? Results.NoContent()
            : result.Error.ToApiResult();
    }

    private static async Task<IResult> GetTrimmedSequence(
        [FromRoute] string studyId,
        [FromRoute] string traceId,
        [FromServices] ISender sender = default!,
        CancellationToken cancellationToken = default)
    {
        var query = new GetTrimmedSequenceQuery(traceId);
        var result = await sender.Send(query, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value.ToResponse())
            : result.Error.ToApiResult();
    }

    private static async Task<IResult> CreateSequenceEdit(
        [FromRoute] string studyId,
        [FromRoute] string traceId,
        [FromBody] CreateSequenceEditRequest request,
        [FromServices] ISender sender = default!,
        [FromServices] ICurrentUserService currentUserService = default!,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
            return Results.Unauthorized();

        var command = new CreateSequenceEditCommand(
            userId,
            traceId,
            request.EditType,
            request.Position,
            request.OriginalBase,
            request.NewBase,
            request.Reason);

        var result = await sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/v1/studies/{studyId}/traces/{traceId}/edits/{result.Value.Id}", result.Value.ToResponse())
            : result.Error.ToApiResult();
    }

    private static async Task<IResult> GetSequenceEdits(
        [FromRoute] string studyId,
        [FromRoute] string traceId,
        [FromQuery] bool includeInactive = false,
        [FromServices] ISender sender = default!,
        CancellationToken cancellationToken = default)
    {
        var query = new GetSequenceEditsQuery(traceId, includeInactive);
        var result = await sender.Send(query, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value.ToResponses())
            : result.Error.ToApiResult();
    }

    private static async Task<IResult> UndoSequenceEdit(
        [FromRoute] string studyId,
        [FromRoute] string traceId,
        [FromRoute] string editId,
        [FromServices] ISender sender = default!,
        [FromServices] ICurrentUserService currentUserService = default!,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
            return Results.Unauthorized();

        var command = new UndoSequenceEditCommand(userId, traceId, editId);
        var result = await sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? Results.NoContent()
            : result.Error.ToApiResult();
    }

    private static async Task<IResult> UndoAllSequenceEdits(
        [FromRoute] string studyId,
        [FromRoute] string traceId,
        [FromServices] ISender sender = default!,
        [FromServices] ICurrentUserService currentUserService = default!,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
            return Results.Unauthorized();

        var command = new UndoAllSequenceEditsCommand(userId, traceId);
        var result = await sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? Results.NoContent()
            : result.Error.ToApiResult();
    }

    private static async Task<IResult> GetEditedSequence(
        [FromRoute] string studyId,
        [FromRoute] string traceId,
        [FromServices] ISender sender = default!,
        CancellationToken cancellationToken = default)
    {
        var query = new GetEditedSequenceQuery(traceId);
        var result = await sender.Send(query, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value.ToResponse())
            : result.Error.ToApiResult();
    }

    private static async Task<IResult> GetReverseComplement(
        [FromRoute] string studyId,
        [FromRoute] string traceId,
        [FromQuery] bool useTrimmedSequence = true,
        [FromServices] ISender sender = default!,
        CancellationToken cancellationToken = default)
    {
        var query = new GetReverseComplementQuery(traceId, useTrimmedSequence);
        var result = await sender.Send(query, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value.ToResponse())
            : result.Error.ToApiResult();
    }
}
