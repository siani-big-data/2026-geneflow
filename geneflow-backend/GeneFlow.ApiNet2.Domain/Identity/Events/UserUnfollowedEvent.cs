using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;

namespace GeneFlow.ApiNet2.Domain.Identity.Events;

public sealed record UserUnfollowedEvent(
    UserId FollowerId,
    UserId FolloweeId) : DomainEvent;
