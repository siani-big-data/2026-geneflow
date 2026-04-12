using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Studies.Commands.LeaveStudy;

/// <summary>
/// Command for a member to leave a study.
/// </summary>
public sealed record LeaveStudyCommand(
    string StudyId,
    string UserId) : ICommand<Result>;
