using GeneFlow.ApiNet2.Domain.Orgs.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Application.EventNotifications;

namespace GeneFlow.ApiNet2.Domain.Orgs.Events;

public sealed record OrgMemberInvitedEvent(
    OrgInvitationId InvitationId,
    OrgId OrgId,
    string InvitedEmail,
    OrgRole Role) : DomainEvent;
