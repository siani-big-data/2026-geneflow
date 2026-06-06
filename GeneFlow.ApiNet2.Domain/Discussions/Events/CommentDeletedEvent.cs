using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;

namespace GeneFlow.ApiNet2.Domain.Discussions.Events;

public sealed record CommentDeletedEvent(
    Guid CommentId,
    UserId DeletedBy) : DomainEvent;
