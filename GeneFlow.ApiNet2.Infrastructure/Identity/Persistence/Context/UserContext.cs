using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Identity.Enumerations;
using Microsoft.EntityFrameworkCore;

namespace GeneFlow.ApiNet2.Infrastructure.Identity.Persistence.Context;

/// <summary>
/// DbContext for Identity bounded context.
/// </summary>
public sealed class UserContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the UserContext.
    /// </summary>
    public UserContext(DbContextOptions<UserContext> options) : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the Users DbSet.
    /// </summary>
    public DbSet<User> Users => Set<User>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("identity");

        // Ignore smart enumerations - they are stored as JSON, not separate entities
        modelBuilder.Ignore<Role>();

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(UserContext).Assembly);
    }
}
