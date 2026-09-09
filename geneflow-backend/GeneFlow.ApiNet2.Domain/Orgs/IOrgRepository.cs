using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Orgs.Entities;

namespace GeneFlow.ApiNet2.Domain.Orgs;

public interface IOrgRepository
{
    Task<Org?> GetByIdAsync(OrgId id, CancellationToken ct = default);

    Task<Org?> GetByHandleAsync(string handle, CancellationToken ct = default);

    Task<bool> HandleExistsAsync(string handle, CancellationToken ct = default);

    Task AddAsync(Org org, CancellationToken ct = default);

    void Update(Org org);

    void Remove(Org org);

    Task<IReadOnlyList<Org>> ListByMemberAsync(UserId userId, CancellationToken ct = default);

    Task<IReadOnlyList<OrgMember>> ListMembersAsync(OrgId orgId, CancellationToken ct = default);

    Task<long> GetNextSequenceValueAsync(CancellationToken ct = default);
}
