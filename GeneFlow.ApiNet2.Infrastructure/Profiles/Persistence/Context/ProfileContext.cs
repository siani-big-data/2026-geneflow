using GeneFlow.ApiNet2.Domain.Profiles;
using GeneFlow.ApiNet2.Domain.Profiles.Entities;
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

    /// <summary>
    /// Studies that users have pinned to their profiles (ordered).
    /// </summary>
    public DbSet<PinnedStudy> PinnedStudies => Set<PinnedStudy>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("profiles");

        // Ignore smart enumerations - they are stored as string, not separate entities
        modelBuilder.Ignore<ResearchField>();

        // Only apply configurations from the Profiles namespace
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ProfileContext).Assembly,
            type => type.Namespace?.Contains("Profiles") == true);
    }
}
