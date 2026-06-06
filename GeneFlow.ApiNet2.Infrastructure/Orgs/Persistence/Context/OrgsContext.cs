using GeneFlow.ApiNet2.Domain.Orgs.Entities;
using GeneFlow.ApiNet2.Domain.Orgs.Enumerations;
using Microsoft.EntityFrameworkCore;
using OrgAggregate = GeneFlow.ApiNet2.Domain.Orgs.Org;

namespace GeneFlow.ApiNet2.Infrastructure.Orgs.Persistence.Context;

/// <summary>
/// DbContext for the Orgs bounded context. Owns the
/// <c>orgs</c> PostgreSQL schema (orgs, org_members, org_invitations).
/// </summary>
public sealed class OrgsContext : DbContext
{
    public OrgsContext(DbContextOptions<OrgsContext> options) : base(options)
    {
    }

    public DbSet<OrgAggregate> Orgs => Set<OrgAggregate>();
    public DbSet<OrgInvitation> OrgInvitations => Set<OrgInvitation>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("orgs");

        // SmartEnums are stored as strings; they are not separate entities.
        modelBuilder.Ignore<OrgRole>();
        modelBuilder.Ignore<OrgVisibility>();
        modelBuilder.Ignore<InvitationStatus>();
        modelBuilder.Ignore<StudyOwnerType>();

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(OrgsContext).Assembly,
            type => type.Namespace?.Contains("Orgs") == true);
    }
}
