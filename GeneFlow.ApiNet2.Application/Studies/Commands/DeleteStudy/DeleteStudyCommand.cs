using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Studies.Commands.DeleteStudy;

/// <summary>
/// Command to soft delete a study.
/// </summary>
public sealed record DeleteStudyCommand(
    string StudyId,
    string UserId) : ICommand<Result>;
