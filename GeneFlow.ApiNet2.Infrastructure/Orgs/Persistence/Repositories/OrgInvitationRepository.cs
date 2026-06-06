using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Orgs;
using GeneFlow.ApiNet2.Domain.Orgs.Entities;
using GeneFlow.ApiNet2.Domain.Orgs.Enumerations;
using GeneFlow.ApiNet2.Infrastructure.Orgs.Persistence.Context;
using GeneFlow.ApiNet2.SharedKernel.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GeneFlow.ApiNet2.Infrastructure.Orgs.Persistence.Repositories;

public sealed class OrgInvitationRepository : IOrgInvitationRepository
{
    private readonly OrgsContext _context;
    private readonly ISequenceGenerator _sequenceGenerator;

    public OrgInvitationRepository(OrgsContext context, ISequenceGenerator sequenceGenerator)
    {
        _context = context;
        _sequenceGenerator = sequenceGenerator;
    }

    public async Task<OrgInvitation?> GetByIdAsync(OrgInvitationId id, CancellationToken ct = default)
    {
        return await _context.OrgInvitations
            .FirstOrDefaultAsync(i => i.Id == id, ct);
    }

    public async Task<OrgInvitation?> GetByTokenAsync(string token, CancellationToken ct = default)
    {
        return await _context.OrgInvitations
            .FirstOrDefaultAsync(i => i.Token == token, ct);
    }

    public async Task AddAsync(OrgInvitation invitation, CancellationToken ct = default)
    {
        await _context.OrgInvitations.AddAsync(invitation, ct);
    }

    public void Update(OrgInvitation invitation)
    {
        _context.OrgInvitations.Update(invitation);
    }

    public async Task<IReadOnlyList<OrgInvitation>> ListPendingForUserAsync(
        UserId userId,
        string email,
        CancellationToken ct = default)
    {
        var normalizedEmail = (email ?? string.Empty).Trim().ToLowerInvariant();

        return await _context.OrgInvitations
            .Where(i => i.Status == InvitationStatus.Pending &&
                        (i.InvitedEmail == normalizedEmail || i.InvitedUserId == userId))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<OrgInvitation>> ListByOrgAsync(
        OrgId orgId,
        CancellationToken ct = default)
    {
        return await _context.OrgInvitations
            .Where(i => i.OrgId == orgId)
            .ToListAsync(ct);
    }

    public Task<long> GetNextSequenceValueAsync(CancellationToken ct = default)
    {
        return _sequenceGenerator.NextAsync(OrgInvitationId.SequenceName, ct);
    }
}
