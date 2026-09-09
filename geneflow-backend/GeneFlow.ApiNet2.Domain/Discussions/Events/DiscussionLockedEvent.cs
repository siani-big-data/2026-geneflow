using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;

namespace GeneFlow.ApiNet2.Domain.Discussions.Events;

public sealed record DiscussionLockedEvent(
    DiscussionId DiscussionId,
    UserId LockedBy) : DomainEvent;
