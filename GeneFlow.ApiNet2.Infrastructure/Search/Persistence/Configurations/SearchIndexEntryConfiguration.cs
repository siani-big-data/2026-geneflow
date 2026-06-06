using GeneFlow.ApiNet2.Domain.Search.Entities;
using GeneFlow.ApiNet2.Domain.Search.Enumerations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GeneFlow.ApiNet2.Infrastructure.Search.Persistence.Configurations;

public sealed class SearchIndexEntryConfiguration : IEntityTypeConfiguration<SearchIndexEntry>
{
    public void Configure(EntityTypeBuilder<SearchIndexEntry> builder)
    {
        builder.ToTable("search_index", "search");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(e => e.ObjectType)
            .HasColumnName("object_type")
            .HasConversion(
                v => v.Id,
                v => SearchObjectType.FromId(v)!)
            .IsRequired();

        builder.Property(e => e.ObjectId)
            .HasColumnName("object_id")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(e => e.OwnerId)
            .HasColumnName("owner_id")
            .HasMaxLength(64);

        builder.Property(e => e.Title)
            .HasColumnName("title")
            .HasMaxLength(SearchIndexEntry.MaxTitleLength)
            .IsRequired();

        builder.Property(e => e.Body)
            .HasColumnName("body")
            .HasColumnType("text");

        builder.Property(e => e.Tags)
            .HasColumnName("tags")
            .HasColumnType("text");

        builder.Property(e => e.IsPublic)
            .HasColumnName("is_public")
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        // The tsvector column is generated server-side from title/body/tags
        // (see AddSearchIndex migration). We don't map it in the entity but
        // do need EF to know about it so the GIN index can be queried via
        // FromSqlRaw without complaining about extra columns.
        builder.HasIndex(e => new { e.ObjectType, e.ObjectId })
            .IsUnique()
            .HasDatabaseName("ix_search_index_object_type_id");

        builder.HasIndex(e => e.UpdatedAt)
            .HasDatabaseName("ix_search_index_updated_at");

        builder.HasIndex(e => e.OwnerId)
            .HasDatabaseName("ix_search_index_owner_id");

        builder.HasIndex(e => e.IsPublic)
            .HasDatabaseName("ix_search_index_is_public");
    }
}
