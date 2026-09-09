using GeneFlow.ApiNet2.Application.Behaviors;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Studies.Commands.LeaveStudy;

/// <summary>
/// Command for a member to leave a study.
/// Requires Viewer role or higher (any member can leave).
/// </summary>
public sealed record LeaveStudyCommand(
    string StudyId,
    string UserId) : ICommand<Result>, IRequireStudyMembership
{
    string IRequireStudyMembership.StudyId => StudyId;
}
