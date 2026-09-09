using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Orgs.Entities;
using GeneFlow.ApiNet2.Infrastructure.Orgs.Persistence.Context;
using GeneFlow.ApiNet2.SharedKernel.Infrastructure;
using Microsoft.EntityFrameworkCore;
using IOrgRepository = GeneFlow.ApiNet2.Domain.Orgs.IOrgRepository;
using OrgAggregate = GeneFlow.ApiNet2.Domain.Orgs.Org;
using OrgIdType = GeneFlow.ApiNet2.Domain.Orgs.OrgId;

namespace GeneFlow.ApiNet2.Infrastructure.Orgs.Persistence.Repositories;

public sealed class OrgRepository : IOrgRepository
{
    private readonly OrgsContext _context;
    private readonly ISequenceGenerator _sequenceGenerator;

    public OrgRepository(OrgsContext context, ISequenceGenerator sequenceGenerator)
    {
        _context = context;
        _sequenceGenerator = sequenceGenerator;
    }

    public async Task<OrgAggregate?> GetByIdAsync(OrgIdType id, CancellationToken ct = default)
    {
        return await _context.Orgs
            .FirstOrDefaultAsync(o => o.Id == id, ct);
    }

    public async Task<OrgAggregate?> GetByHandleAsync(string handle, CancellationToken ct = default)
    {
        return await _context.Orgs
            .FirstOrDefaultAsync(o => o.Handle == handle, ct);
    }

    public async Task<bool> HandleExistsAsync(string handle, CancellationToken ct = default)
    {
        return await _context.Orgs.AnyAsync(o => o.Handle == handle, ct);
    }

    public async Task AddAsync(OrgAggregate org, CancellationToken ct = default)
    {
        await _context.Orgs.AddAsync(org, ct);
    }

    public void Update(OrgAggregate org)
    {
        _context.Orgs.Update(org);
    }

    public void Remove(OrgAggregate org)
    {
        _context.Orgs.Remove(org);
    }

    public async Task<IReadOnlyList<OrgAggregate>> ListByMemberAsync(UserId userId, CancellationToken ct = default)
    {
        return await _context.Orgs
            .Where(o => o.Members.Any(m => m.UserId == userId))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<OrgMember>> ListMembersAsync(OrgIdType orgId, CancellationToken ct = default)
    {
        var org = await _context.Orgs
            .FirstOrDefaultAsync(o => o.Id == orgId, ct);

        if (org is null)
            return Array.Empty<OrgMember>();

        return org.Members.ToList();
    }

    public Task<long> GetNextSequenceValueAsync(CancellationToken ct = default)
    {
        return _sequenceGenerator.NextAsync(OrgIdType.SequenceName, ct);
    }
}
