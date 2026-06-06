using GeneFlow.ApiNet2.Application.Behaviors;
using GeneFlow.ApiNet2.Application.Studies.DTOs;
using GeneFlow.ApiNet2.Domain.Studies.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Studies.Commands.UpdateReadme;

/// <summary>
/// Command to update a study's README markdown content.
/// Requires Admin role or higher in the study.
/// </summary>
public sealed record UpdateReadmeCommand(
    string StudyId,
    string UserId,
    string? Markdown) : ICommand<Result<StudyDto>>, IRequireStudyMembership
{
    string IRequireStudyMembership.StudyId => StudyId;
    StudyRole? IRequireStudyMembership.MinimumRole => StudyRole.Admin;
}
