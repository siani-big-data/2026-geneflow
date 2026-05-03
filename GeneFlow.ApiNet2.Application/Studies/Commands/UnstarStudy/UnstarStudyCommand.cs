using GeneFlow.ApiNet2.Application.Behaviors;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Studies.Commands.UnstarStudy;

/// <summary>
/// Command to unstar (remove from favorites) a study.
/// Requires authentication.
/// </summary>
public sealed record UnstarStudyCommand(
    string StudyId,
    string UserId) : ICommand<Result>, IRequireAuthentication;
