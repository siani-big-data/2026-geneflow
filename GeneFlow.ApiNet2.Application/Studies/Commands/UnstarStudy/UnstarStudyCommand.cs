using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Studies.Commands.UnstarStudy;

/// <summary>
/// Command to unstar (remove from favorites) a study.
/// </summary>
public sealed record UnstarStudyCommand(
    string StudyId,
    string UserId) : ICommand<Result>;
