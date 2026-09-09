using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Studies.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GeneFlow.ApiNet2.Infrastructure.Studies.Persistence.Configurations;

/// <summary>
/// EF Core configuration for StudyView entity.
/// </summary>
public sealed class StudyViewConfiguration : IEntityTypeConfiguration<StudyView>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<StudyView> builder)
    {
        builder.ToTable("study_views");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        // StudyId
        builder.Property(v => v.StudyId)
            .HasColumnName("study_id")
            .HasMaxLength(10)
            .HasConversion(
                id => id.ToString(),
                value => StudyId.Parse(value))
            .IsRequired();

        builder.HasIndex(v => v.StudyId);
        builder.HasIndex(v => new { v.StudyId, v.ViewedAt });

        // UserId (nullable for anonymous)
        builder.Property(v => v.UserId)
            .HasColumnName("user_id")
            .HasMaxLength(10)
            .HasConversion(
                id => id != null ? id.ToString() : null,
                value => value != null ? UserId.Parse(value) : null);

        // IpHash (for deduplication)
        builder.Property(v => v.IpHash)
            .HasColumnName("ip_hash")
            .HasMaxLength(64);

        builder.HasIndex(v => new { v.StudyId, v.IpHash, v.ViewedAt });

        // UserAgent
        builder.Property(v => v.UserAgent)
            .HasColumnName("user_agent")
            .HasMaxLength(500);

        // ViewedAt
        builder.Property(v => v.ViewedAt)
            .HasColumnName("viewed_at")
            .IsRequired();
    }
}
