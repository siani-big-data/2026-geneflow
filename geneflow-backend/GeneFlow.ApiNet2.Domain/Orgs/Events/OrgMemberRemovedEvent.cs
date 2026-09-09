using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;

namespace GeneFlow.ApiNet2.Domain.Orgs.Events;

public sealed record OrgMemberRemovedEvent(
    OrgId OrgId,
    UserId UserId,
    UserId RemovedByUserId) : DomainEvent;
