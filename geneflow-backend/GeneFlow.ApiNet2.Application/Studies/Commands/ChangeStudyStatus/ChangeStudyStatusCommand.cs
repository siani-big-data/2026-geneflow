using GeneFlow.ApiNet2.Application.Behaviors;
using GeneFlow.ApiNet2.Application.Studies.DTOs;
using GeneFlow.ApiNet2.Domain.Studies.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Studies.Commands.ChangeStudyStatus;

/// <summary>
/// Command to change the status of a study.
/// Requires Owner role in the study.
/// </summary>
public sealed record ChangeStudyStatusCommand(
    string StudyId,
    string UserId,
    int NewStatusId) : ICommand<Result<StudyDto>>, IRequireStudyMembership
{
    string IRequireStudyMembership.StudyId => StudyId;
    StudyRole? IRequireStudyMembership.MinimumRole => StudyRole.Owner;
}
