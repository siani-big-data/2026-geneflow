using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Studies.Commands.RemoveStudyPaper;

/// <summary>
/// Command to remove (soft delete) a paper from a study.
/// </summary>
public sealed record RemoveStudyPaperCommand(
    string StudyId,
    string PaperId,
    string UserId) : ICommand<Result>;
