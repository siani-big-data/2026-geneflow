using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Studies.Commands.StarStudy;

/// <summary>
/// Command to star (favorite) a study.
/// </summary>
public sealed record StarStudyCommand(
    string StudyId,
    string UserId) : ICommand<Result>;
