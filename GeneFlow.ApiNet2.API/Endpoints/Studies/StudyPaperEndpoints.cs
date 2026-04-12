using GeneFlow.ApiNet2.API.Contracts.Studies.Requests;
using GeneFlow.ApiNet2.API.Contracts.Studies.Responses;
using GeneFlow.ApiNet2.API.Extensions;
using GeneFlow.ApiNet2.Application.Identity.Interfaces;
using GeneFlow.ApiNet2.Application.Studies.Commands.AddStudyPaper;
using GeneFlow.ApiNet2.Application.Studies.Commands.RemoveStudyPaper;
using GeneFlow.ApiNet2.Application.Studies.Queries.GetStudyPapers;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GeneFlow.ApiNet2.API.Endpoints.Studies;

/// <summary>
/// Study paper management endpoints.
/// </summary>
public sealed class StudyPaperEndpoints : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/studies/{studyId}/papers")
            .WithTags("Study Papers")
            .WithOpenApi()
            .RequireAuthorization();

        group.MapGet("/", GetStudyPapers)
            .WithName("StudyPapers_Get")
            .WithSummary("Get study papers")
            .WithDescription("Returns all papers associated with a study.")
            .Produces<IReadOnlyList<StudyPaperResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiError>(StatusCodes.Status403Forbidden)
            .Produces<ApiError>(StatusCodes.Status404NotFound);

        group.MapPost("/", AddStudyPaper)
            .WithName("StudyPapers_Add")
            .WithSummary("Add a paper to a study")
            .WithDescription("Adds a new paper to the study. File upload via multipart/form-data is supported separately.")
            .Produces<StudyPaperResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiError>(StatusCodes.Status403Forbidden)
            .Produces<ApiError>(StatusCodes.Status404NotFound);

        group.MapDelete("/{paperId}", RemoveStudyPaper)
            .WithName("StudyPapers_Remove")
            .WithSummary("Remove a paper from a study")
            .WithDescription("Removes (soft-deletes) a paper from the study.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<ApiError>(StatusCodes.Status403Forbidden)
            .Produces<ApiError>(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> GetStudyPapers(
        [FromRoute] string studyId,
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var query = new GetStudyPapersQuery(studyId, currentUser.UserId.ToString()!);
        var result = await sender.Send(query, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.Ok(result.Value.ToResponses());
    }

    private static async Task<IResult> AddStudyPaper(
        [FromRoute] string studyId,
        [FromBody] AddStudyPaperRequest request,
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var command = new AddStudyPaperCommand(
            studyId,
            currentUser.UserId.ToString()!,
            request.Title,
            request.Authors,
            request.Doi,
            request.Abstract,
            request.Journal,
            request.PublicationYear,
            null, // FileId - would be set via separate file upload
            null, // FileName
            null); // FileSizeBytes

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.Created($"/api/v1/studies/{studyId}/papers/{result.Value.Id}", result.Value.ToResponse());
    }

    private static async Task<IResult> RemoveStudyPaper(
        [FromRoute] string studyId,
        [FromRoute] string paperId,
        [FromServices] ISender sender,
        [FromServices] ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Results.Unauthorized();

        var command = new RemoveStudyPaperCommand(
            studyId,
            paperId,
            currentUser.UserId.ToString()!);

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return result.ToHttpResult();

        return Results.NoContent();
    }
}
