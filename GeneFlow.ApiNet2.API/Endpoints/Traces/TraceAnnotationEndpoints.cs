using GeneFlow.ApiNet2.API.Contracts.Traces.Requests;
using GeneFlow.ApiNet2.API.Contracts.Traces.Responses;
using GeneFlow.ApiNet2.API.Extensions;
using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Application.Traces.Commands.CreateAnnotation;
using GeneFlow.ApiNet2.Application.Traces.Commands.DeleteAnnotation;
using GeneFlow.ApiNet2.Application.Traces.Commands.UpdateAnnotation;
using GeneFlow.ApiNet2.Application.Traces.Queries.GetStudyAnnotations;
using GeneFlow.ApiNet2.Application.Traces.Queries.GetTraceAnnotations;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GeneFlow.ApiNet2.API.Endpoints.Traces;

/// <summary>
/// Trace annotation endpoints.
/// </summary>
public sealed class TraceAnnotationEndpoints : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        // Study-level annotations (shared)
        var studyGroup = app.MapGroup("/api/v1/studies/{studyId}/annotations")
            .WithTags("Trace Annotations")
            .WithOpenApi()
            .RequireAuthorization();

        studyGroup.MapGet("/", GetStudyAnnotations)
            .WithName("Traces_GetStudyAnnotations")
            .WithSummary("Get shared annotations for a study")
            .WithDescription("Returns all shared annotations across all traces in a study.")
            .Produces<IReadOnlyList<AnnotationResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiError>(StatusCodes.Status404NotFound);

        // Trace-level annotations
        var traceGroup = app.MapGroup("/api/v1/studies/{studyId}/traces/{traceId}/annotations")
            .WithTags("Trace Annotations")
            .WithOpenApi()
            .RequireAuthorization();

        traceGroup.MapPost("/", CreateAnnotation)
            .WithName("Traces_CreateAnnotation")
            .WithSummary("Create annotation")
            .WithDescription("Creates a new annotation on a trace.")
            .Produces<AnnotationResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiError>(StatusCodes.Status404NotFound);

        traceGroup.MapGet("/", GetTraceAnnotations)
            .WithName("Traces_GetAnnotations")
            .WithSummary("Get trace annotations")
            .WithDescription("Returns all annotations for a trace.")
            .Produces<IReadOnlyList<AnnotationResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiError>(StatusCodes.Status404NotFound);

        traceGroup.MapPut("/{annotationId}", UpdateAnnotation)
            .WithName("Traces_UpdateAnnotation")
            .WithSummary("Update annotation")
            .WithDescription("Updates an existing annotation on a trace.")
            .Produces<AnnotationResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiError>(StatusCodes.Status404NotFound);

        traceGroup.MapDelete("/{annotationId}", DeleteAnnotation)
            .WithName("Traces_DeleteAnnotation")
            .WithSummary("Delete annotation")
            .WithDescription("Deletes an annotation from a trace.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiError>(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> GetStudyAnnotations(
        [FromRoute] string studyId,
        [FromServices] ISender sender = default!,
        CancellationToken cancellationToken = default)
    {
        var query = new GetStudyAnnotationsQuery(studyId);
        var result = await sender.Send(query, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value.ToResponses())
            : result.Error.ToApiResult();
    }

    private static async Task<IResult> CreateAnnotation(
        [FromRoute] string studyId,
        [FromRoute] string traceId,
        [FromBody] CreateAnnotationRequest request,
        [FromServices] ISender sender = default!,
        [FromServices] ICurrentUserService currentUserService = default!,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
            return Results.Unauthorized();

        var command = new CreateAnnotationCommand(
            userId,
            traceId,
            request.TypeId,
            request.Label,
            request.Description,
            request.StartPosition,
            request.EndPosition,
            request.StrandId,
            request.Color,
            request.IsShared,
            request.Metadata);

        var result = await sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/v1/studies/{studyId}/traces/{traceId}/annotations/{result.Value.Id}", result.Value.ToResponse())
            : result.Error.ToApiResult();
    }

    private static async Task<IResult> GetTraceAnnotations(
        [FromRoute] string studyId,
        [FromRoute] string traceId,
        [FromServices] ISender sender = default!,
        CancellationToken cancellationToken = default)
    {
        var query = new GetTraceAnnotationsQuery(traceId);
        var result = await sender.Send(query, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value.ToResponses())
            : result.Error.ToApiResult();
    }

    private static async Task<IResult> UpdateAnnotation(
        [FromRoute] string studyId,
        [FromRoute] string traceId,
        [FromRoute] string annotationId,
        [FromBody] UpdateAnnotationRequest request,
        [FromServices] ISender sender = default!,
        [FromServices] ICurrentUserService currentUserService = default!,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
            return Results.Unauthorized();

        var command = new UpdateAnnotationCommand(
            userId,
            traceId,
            annotationId,
            request.Label,
            request.Description,
            request.StartPosition,
            request.EndPosition,
            request.StrandId,
            request.Color,
            request.IsShared,
            request.Metadata);

        var result = await sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value.ToResponse())
            : result.Error.ToApiResult();
    }

    private static async Task<IResult> DeleteAnnotation(
        [FromRoute] string studyId,
        [FromRoute] string traceId,
        [FromRoute] string annotationId,
        [FromServices] ISender sender = default!,
        [FromServices] ICurrentUserService currentUserService = default!,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
            return Results.Unauthorized();

        var command = new DeleteAnnotationCommand(userId, traceId, annotationId);
        var result = await sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? Results.NoContent()
            : result.Error.ToApiResult();
    }
}
