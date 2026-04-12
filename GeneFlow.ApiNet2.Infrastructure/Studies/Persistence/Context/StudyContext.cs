using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Studies.Entities;
using GeneFlow.ApiNet2.Domain.Studies.Enumerations;
using Microsoft.EntityFrameworkCore;

namespace GeneFlow.ApiNet2.Infrastructure.Studies.Persistence.Context;

/// <summary>
/// DbContext for Studies bounded context.
/// </summary>
public sealed class StudyContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the StudyContext.
    /// </summary>
    public StudyContext(DbContextOptions<StudyContext> options) : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the Studies DbSet.
    /// </summary>
    public DbSet<Study> Studies => Set<Study>();

    /// <summary>
    /// Gets or sets the Study Invitations DbSet.
    /// </summary>
    public DbSet<StudyInvitation> StudyInvitations => Set<StudyInvitation>();

    /// <summary>
    /// Gets or sets the Study Stars DbSet.
    /// </summary>
    public DbSet<StudyStar> StudyStars => Set<StudyStar>();

    /// <summary>
    /// Gets or sets the Study Views DbSet.
    /// </summary>
    public DbSet<StudyView> StudyViews => Set<StudyView>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("studies");

        // Ignore smart enumerations - they are stored as string, not separate entities
        modelBuilder.Ignore<StudyStatus>();
        modelBuilder.Ignore<StudyRole>();
        modelBuilder.Ignore<ResearchField>();
        modelBuilder.Ignore<InvitationStatus>();

        // Only apply configurations from the Studies namespace
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(StudyContext).Assembly,
            type => type.Namespace?.Contains("Studies") == true);
    }
}
