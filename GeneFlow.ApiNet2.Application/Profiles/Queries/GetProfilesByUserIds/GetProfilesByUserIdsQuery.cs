using GeneFlow.ApiNet2.Application.Profiles.DTOs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Profiles.Queries.GetProfilesByUserIds;

/// <summary>
/// Query to get multiple profiles by user IDs.
/// </summary>
public sealed record GetProfilesByUserIdsQuery(
    IReadOnlyList<string> UserIds) : IQuery<Result<IReadOnlyList<ProfileSummaryDto>>>;
