using GeneFlow.ApiNet2.Application.Behaviors;
using GeneFlow.ApiNet2.Domain.Studies.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Studies.Commands.RemoveStudyPaper;

/// <summary>
/// Command to remove (soft delete) a paper from a study.
/// Requires Editor role or higher.
/// </summary>
public sealed record RemoveStudyPaperCommand(
    string StudyId,
    string PaperId,
    string UserId) : ICommand<Result>, IRequireStudyMembership
{
    string IRequireStudyMembership.StudyId => StudyId;
    StudyRole? IRequireStudyMembership.MinimumRole => StudyRole.Editor;
}
