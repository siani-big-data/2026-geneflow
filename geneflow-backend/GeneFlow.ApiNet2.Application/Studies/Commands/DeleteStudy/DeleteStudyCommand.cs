using GeneFlow.ApiNet2.Application.Behaviors;
using GeneFlow.ApiNet2.Domain.Studies.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Studies.Commands.DeleteStudy;

/// <summary>
/// Command to soft delete a study.
/// Requires Owner role in the study.
/// </summary>
public sealed record DeleteStudyCommand(
    string StudyId,
    string UserId) : ICommand<Result>, IRequireStudyMembership
{
    string IRequireStudyMembership.StudyId => StudyId;
    StudyRole? IRequireStudyMembership.MinimumRole => StudyRole.Owner;
}
