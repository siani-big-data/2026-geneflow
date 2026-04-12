using GeneFlow.ApiNet2.Application.Studies.DTOs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Studies.Commands.FeatureStudy;

/// <summary>
/// Command to feature or unfeature a study (admin only).
/// </summary>
public sealed record FeatureStudyCommand(
    string StudyId,
    string AdminUserId,
    bool IsFeatured) : ICommand<Result<StudyDto>>;
