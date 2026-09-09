using GeneFlow.ApiNet2.Domain.Discussions.Enumerations;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;

namespace GeneFlow.ApiNet2.Domain.Discussions.Events;

public sealed record CommentCreatedEvent(
    Guid CommentId,
    CommentParentType ParentType,
    string ParentId,
    UserId AuthorId,
    string BodyMarkdown) : DomainEvent;
