using GeneFlow.ApiNet2.Domain.Discussions;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GeneFlow.ApiNet2.Infrastructure.Discussions.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the Discussion aggregate.
/// </summary>
public sealed class DiscussionConfiguration : IEntityTypeConfiguration<Discussion>
{
    public void Configure(EntityTypeBuilder<Discussion> builder)
    {
        builder.ToTable("discussions");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Id)
            .HasColumnName("id")
            .HasMaxLength(10)
            .HasConversion(
                id => id.ToString(),
                value => DiscussionId.Parse(value));

        builder.Property(d => d.StudyId)
            .HasColumnName("study_id")
            .HasMaxLength(10)
            .HasConversion(
                id => id.ToString(),
                value => StudyId.Parse(value))
            .IsRequired();

        builder.HasIndex(d => d.StudyId);

        builder.Property(d => d.AuthorId)
            .HasColumnName("author_id")
            .HasMaxLength(10)
            .HasConversion(
                id => id.ToString(),
                value => UserId.Parse(value))
            .IsRequired();

        builder.HasIndex(d => d.AuthorId);

        builder.Property(d => d.Title)
            .HasColumnName("title")
            .HasMaxLength(Discussion.MaxTitleLength)
            .IsRequired();

        builder.Property(d => d.Category)
            .HasColumnName("category")
            .HasMaxLength(Discussion.MaxCategoryLength);

        builder.Property(d => d.IsLocked)
            .HasColumnName("is_locked")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(d => d.LockedAt)
            .HasColumnName("locked_at");

        builder.Property(d => d.LockedBy)
            .HasColumnName("locked_by")
            .HasMaxLength(10)
            .HasConversion(
                id => id == null ? null : id.ToString(),
                value => value == null ? null : UserId.Parse(value));

        // Auditing
        builder.Property(d => d.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(d => d.CreatedBy)
            .HasColumnName("created_by")
            .HasMaxLength(100);

        builder.Property(d => d.ModifiedAt)
            .HasColumnName("modified_at");

        builder.Property(d => d.ModifiedBy)
            .HasColumnName("modified_by")
            .HasMaxLength(100);

        builder.Property(d => d.IsDeleted)
            .HasColumnName("is_deleted")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(d => d.DeletedAt)
            .HasColumnName("deleted_at");

        builder.Property(d => d.DeletedBy)
            .HasColumnName("deleted_by")
            .HasMaxLength(100);

        // Soft delete query filter.
        builder.HasQueryFilter(d => !d.IsDeleted);

        // Composite browse index for the per-study list endpoint.
        builder.HasIndex(d => new { d.StudyId, d.CreatedAt });

        builder.Ignore(d => d.DomainEvents);
    }
}
