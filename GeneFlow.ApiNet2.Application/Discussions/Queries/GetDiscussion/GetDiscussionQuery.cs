using GeneFlow.ApiNet2.Application.Behaviors;
using GeneFlow.ApiNet2.Application.Discussions.DTOs;
using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Discussions.Queries.GetDiscussion;

/// <summary>
/// Returns a discussion together with all of its (non-deleted) comments
/// and reaction summaries. Authorization is enforced inside the handler
/// because the study ID isn't on the request payload — we look it up
/// from the discussion first.
/// </summary>
public sealed record GetDiscussionQuery(
    string DiscussionId,
    string? UserId) : IQuery<Result<DiscussionDto>>;
