using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Orgs.Entities;

namespace GeneFlow.ApiNet2.Domain.Orgs;

public interface IOrgInvitationRepository
{
    Task<OrgInvitation?> GetByIdAsync(OrgInvitationId id, CancellationToken ct = default);

    Task<OrgInvitation?> GetByTokenAsync(string token, CancellationToken ct = default);

    Task AddAsync(OrgInvitation invitation, CancellationToken ct = default);

    void Update(OrgInvitation invitation);

    Task<IReadOnlyList<OrgInvitation>> ListPendingForUserAsync(
        UserId userId,
        string email,
        CancellationToken ct = default);

    Task<IReadOnlyList<OrgInvitation>> ListByOrgAsync(OrgId orgId, CancellationToken ct = default);

    Task<long> GetNextSequenceValueAsync(CancellationToken ct = default);
}
