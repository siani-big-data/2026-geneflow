using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Studies.Queries.IsStudyStarred;

/// <summary>
/// Query to check if a study is starred by the current user.
/// </summary>
public sealed record IsStudyStarredQuery(
    string StudyId,
    string UserId) : IQuery<Result<bool>>;
