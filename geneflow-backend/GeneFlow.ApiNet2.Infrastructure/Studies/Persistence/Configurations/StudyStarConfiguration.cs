using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Studies.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GeneFlow.ApiNet2.Infrastructure.Studies.Persistence.Configurations;

/// <summary>
/// EF Core configuration for StudyStar entity.
/// </summary>
public sealed class StudyStarConfiguration : IEntityTypeConfiguration<StudyStar>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<StudyStar> builder)
    {
        builder.ToTable("study_stars");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .HasColumnName("id");

        // StudyId
        builder.Property(s => s.StudyId)
            .HasColumnName("study_id")
            .HasMaxLength(10)
            .HasConversion(
                id => id.ToString(),
                value => StudyId.Parse(value))
            .IsRequired();

        builder.HasIndex(s => s.StudyId);

        // UserId
        builder.Property(s => s.UserId)
            .HasColumnName("user_id")
            .HasMaxLength(10)
            .HasConversion(
                id => id.ToString(),
                value => UserId.Parse(value))
            .IsRequired();

        builder.HasIndex(s => s.UserId);

        // Composite unique index
        builder.HasIndex(s => new { s.StudyId, s.UserId }).IsUnique();

        // StarredAt
        builder.Property(s => s.StarredAt)
            .HasColumnName("starred_at")
            .IsRequired();
    }
}
