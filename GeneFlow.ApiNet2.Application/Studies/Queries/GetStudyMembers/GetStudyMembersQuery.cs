using GeneFlow.ApiNet2.Application.Behaviors;
using GeneFlow.ApiNet2.Application.Studies.DTOs;
using GeneFlow.ApiNet2.Domain.Studies.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Studies.Queries.GetStudyMembers;

/// <summary>
/// Query to get all members of a study.
/// Requires Viewer role or higher.
/// </summary>
public sealed record GetStudyMembersQuery(
    string StudyId,
    string? UserId = null) : IQuery<Result<IReadOnlyList<StudyMemberDto>>>, IRequireStudyMembership
{
    string IRequireStudyMembership.StudyId => StudyId;
    StudyRole? IRequireStudyMembership.MinimumRole => StudyRole.Viewer;
}
