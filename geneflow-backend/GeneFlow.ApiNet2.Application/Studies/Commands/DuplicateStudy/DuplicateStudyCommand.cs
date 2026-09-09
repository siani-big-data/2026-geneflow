using GeneFlow.ApiNet2.Application.Behaviors;
using GeneFlow.ApiNet2.Application.Studies.DTOs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Studies.Commands.DuplicateStudy;

/// <summary>
/// Command to duplicate an existing study.
/// Requires Viewer role or higher in the source study and validates study creation limit.
/// </summary>
public sealed record DuplicateStudyCommand(
    string StudyId,
    string UserId) : ICommand<Result<StudyDto>>, IRequireStudyMembership, IRequiresStudyLimit
{
    string IRequireStudyMembership.StudyId => StudyId;
}
