using GeneFlow.ApiNet2.Application.Usage.DTOs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Usage.Queries.GetBillingUsage;

/// <summary>
/// Query to get billing usage for the current user.
/// </summary>
public sealed record GetBillingUsageQuery(string UserId) : IQuery<Result<BillingUsageDto>>;
