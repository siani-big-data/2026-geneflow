using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Notifications.Entities;
using GeneFlow.ApiNet2.Domain.Notifications.Enumerations;
using GeneFlow.ApiNet2.Domain.Studies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GeneFlow.ApiNet2.Infrastructure.Notifications.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the Watch aggregate. (UserId, StudyId)
/// uniqueness is enforced by a unique composite index.
/// </summary>
public sealed class WatchConfiguration : IEntityTypeConfiguration<Watch>
{
    public void Configure(EntityTypeBuilder<Watch> builder)
    {
        builder.ToTable("watches");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(w => w.UserId)
            .HasColumnName("user_id")
            .HasMaxLength(10)
            .HasConversion(
                id => id.ToString(),
                value => UserId.Parse(value))
            .IsRequired();

        builder.Property(w => w.StudyId)
            .HasColumnName("study_id")
            .HasMaxLength(10)
            .HasConversion(
                id => id.ToString(),
                value => StudyId.Parse(value))
            .IsRequired();

        builder.Property(w => w.Level)
            .HasColumnName("level")
            .HasMaxLength(20)
            .HasConversion(
                lvl => lvl.Name,
                name => WatchLevel.FromName(name))
            .IsRequired();

        builder.Property(w => w.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(w => w.ModifiedAt)
            .HasColumnName("modified_at");

        builder.HasIndex(w => new { w.UserId, w.StudyId })
            .IsUnique();

        // Fan-out lookup: every watcher of a study at a given level.
        builder.HasIndex(w => new { w.StudyId, w.Level });

        builder.Ignore(w => w.DomainEvents);
    }
}
