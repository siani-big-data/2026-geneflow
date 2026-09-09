using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;

namespace GeneFlow.ApiNet2.Domain.Orgs.Events;

public sealed record OrgCreatedEvent(
    OrgId OrgId,
    string Handle,
    string Name,
    UserId CreatorUserId) : DomainEvent;
