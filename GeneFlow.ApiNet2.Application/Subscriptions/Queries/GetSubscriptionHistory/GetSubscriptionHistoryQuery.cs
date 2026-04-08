using GeneFlow.ApiNet2.Application.Subscriptions.DTOs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Subscriptions.Queries.GetSubscriptionHistory;

/// <summary>
/// Query to get user's subscription history.
/// </summary>
public sealed record GetSubscriptionHistoryQuery(string UserId) : IQuery<Result<IReadOnlyList<SubscriptionSummaryDto>>>;
