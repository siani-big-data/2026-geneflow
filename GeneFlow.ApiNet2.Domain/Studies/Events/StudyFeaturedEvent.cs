using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;

namespace GeneFlow.ApiNet2.Domain.Studies.Events;

public sealed record StudyFeaturedEvent(
    StudyId StudyId,
    UserId FeaturedBy) : DomainEvent;
