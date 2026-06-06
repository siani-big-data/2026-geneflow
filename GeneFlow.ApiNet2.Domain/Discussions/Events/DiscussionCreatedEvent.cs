using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;

namespace GeneFlow.ApiNet2.Domain.Discussions.Events;

public sealed record DiscussionCreatedEvent(
    DiscussionId DiscussionId,
    StudyId StudyId,
    UserId AuthorId,
    string Title) : DomainEvent;
