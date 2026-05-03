using GeneFlow.ApiNet2.Application.Behaviors;
using GeneFlow.ApiNet2.Application.Studies.DTOs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Studies.Queries.GetStudyPapers;

/// <summary>
/// Query to get all papers of a study.
/// Requires membership in the study (allows public study access).
/// </summary>
public sealed record GetStudyPapersQuery(
    string StudyId,
    string? UserId = null) : IQuery<Result<IReadOnlyList<StudyPaperDto>>>, IRequireStudyMembership
{
    string IRequireStudyMembership.StudyId => StudyId;
}
