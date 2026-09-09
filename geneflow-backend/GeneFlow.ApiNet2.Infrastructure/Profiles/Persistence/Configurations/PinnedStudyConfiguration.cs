using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Profiles.Entities;
using GeneFlow.ApiNet2.Domain.Studies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GeneFlow.ApiNet2.Infrastructure.Profiles.Persistence.Configurations;

/// <summary>
/// EF Core configuration for PinnedStudy.
/// </summary>
public sealed class PinnedStudyConfiguration : IEntityTypeConfiguration<PinnedStudy>
{
    public void Configure(EntityTypeBuilder<PinnedStudy> builder)
    {
        builder.ToTable("pinned_studies");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasColumnName("id");

        builder.Property(p => p.UserId)
            .HasColumnName("user_id")
            .HasMaxLength(10)
            .HasConversion(
                id => id.ToString(),
                value => UserId.Parse(value))
            .IsRequired();

        builder.HasIndex(p => p.UserId);

        builder.Property(p => p.StudyId)
            .HasColumnName("study_id")
            .HasMaxLength(10)
            .HasConversion(
                id => id.ToString(),
                value => StudyId.Parse(value))
            .IsRequired();

        // Unique constraint: a user cannot pin the same study twice.
        builder.HasIndex(p => new { p.UserId, p.StudyId }).IsUnique();

        builder.Property(p => p.Order)
            .HasColumnName("order")
            .IsRequired();

        builder.Property(p => p.PinnedAt)
            .HasColumnName("pinned_at")
            .IsRequired();
    }
}
