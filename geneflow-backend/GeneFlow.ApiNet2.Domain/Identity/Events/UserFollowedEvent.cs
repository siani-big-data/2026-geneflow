using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;

namespace GeneFlow.ApiNet2.Domain.Identity.Events;

public sealed record UserFollowedEvent(
    UserId FollowerId,
    UserId FolloweeId) : DomainEvent;
