using GeneFlow.ApiNet2.Application.Behaviors;
using GeneFlow.ApiNet2.Application.Studies.DTOs;
using GeneFlow.ApiNet2.Domain.Studies.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Studies.Commands.AddStudyPaper;

/// <summary>
/// Command to add a paper to a study.
/// Requires Editor role or higher.
/// </summary>
public sealed record AddStudyPaperCommand(
    string StudyId,
    string UserId,
    string Title,
    string? Authors = null,
    string? Doi = null,
    string? Abstract = null,
    string? Journal = null,
    int? PublicationYear = null,
    string? FileId = null,
    string? FileName = null,
    long? FileSizeBytes = null) : ICommand<Result<StudyPaperDto>>, IRequireStudyMembership
{
    string IRequireStudyMembership.StudyId => StudyId;
    StudyRole? IRequireStudyMembership.MinimumRole => StudyRole.Editor;
}
