using GeneFlow.ApiNet2.Domain.Discussions;
using GeneFlow.ApiNet2.Domain.Discussions.Entities;
using GeneFlow.ApiNet2.Domain.Discussions.Enumerations;
using Microsoft.EntityFrameworkCore;

namespace GeneFlow.ApiNet2.Infrastructure.Discussions.Persistence.Context;

/// <summary>
/// DbContext for the Discussions bounded context. Owns the
/// <c>discussions</c> PostgreSQL schema (discussions, comments, reactions).
/// </summary>
public sealed class DiscussionContext : DbContext
{
    public DiscussionContext(DbContextOptions<DiscussionContext> options) : base(options)
    {
    }

    public DbSet<Discussion> Discussions => Set<Discussion>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Reaction> Reactions => Set<Reaction>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("discussions");

        // SmartEnums are stored as strings; they are not separate entities.
        modelBuilder.Ignore<CommentParentType>();

        // Only the Discussions namespace configurations.
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(DiscussionContext).Assembly,
            type => type.Namespace?.Contains("Discussions") == true);
    }
}
