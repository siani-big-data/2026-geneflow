using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Orgs.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;

namespace GeneFlow.ApiNet2.Domain.Orgs.Events;

public sealed record OrgMemberRoleChangedEvent(
    OrgId OrgId,
    UserId UserId,
    OrgRole OldRole,
    OrgRole NewRole,
    UserId ChangedByUserId) : DomainEvent;
