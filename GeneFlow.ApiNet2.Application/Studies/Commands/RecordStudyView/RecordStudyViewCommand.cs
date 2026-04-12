using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Studies.Commands.RecordStudyView;

/// <summary>
/// Command to record a view on a study.
/// </summary>
public sealed record RecordStudyViewCommand(
    string StudyId,
    string? UserId = null,
    string? IpHash = null,
    string? UserAgent = null) : ICommand<Result>;
