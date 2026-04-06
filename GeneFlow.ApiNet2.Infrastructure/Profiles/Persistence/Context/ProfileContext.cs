using GeneFlow.ApiNet2.Domain.Profiles;
using GeneFlow.ApiNet2.Domain.Profiles.Enumerations;
using Microsoft.EntityFrameworkCore;

namespace GeneFlow.ApiNet2.Infrastructure.Profiles.Persistence.Context;

/// <summary>
/// DbContext for Profiles bounded context.
/// </summary>
public sealed class ProfileContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the ProfileContext.
    /// </summary>
    public ProfileContext(DbContextOptions<ProfileContext> options) : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the Profiles DbSet.
    /// </summary>
    public DbSet<Profile> Profiles => Set<Profile>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("profiles");

        // Ignore smart enumerations - they are stored as string, not separate entities
        modelBuilder.Ignore<ResearchField>();

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProfileContext).Assembly);
    }
}
