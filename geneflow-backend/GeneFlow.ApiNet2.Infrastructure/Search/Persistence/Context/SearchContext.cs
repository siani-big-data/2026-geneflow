using GeneFlow.ApiNet2.Domain.Search.Entities;
using GeneFlow.ApiNet2.Domain.Search.Enumerations;
using Microsoft.EntityFrameworkCore;

namespace GeneFlow.ApiNet2.Infrastructure.Search.Persistence.Context;

/// <summary>
/// DbContext for the Search bounded context. Owns the <c>search</c>
/// PostgreSQL schema (search_index).
/// </summary>
public sealed class SearchContext : DbContext
{
    public SearchContext(DbContextOptions<SearchContext> options) : base(options)
    {
    }

    public DbSet<SearchIndexEntry> SearchIndex => Set<SearchIndexEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("search");

        modelBuilder.Ignore<SearchObjectType>();

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(SearchContext).Assembly,
            type => type.Namespace?.Contains("Search") == true);
    }
}
