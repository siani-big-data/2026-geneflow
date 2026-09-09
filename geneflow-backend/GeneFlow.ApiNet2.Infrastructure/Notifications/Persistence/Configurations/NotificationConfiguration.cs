using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Notifications;
using GeneFlow.ApiNet2.Domain.Notifications.Enumerations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GeneFlow.ApiNet2.Infrastructure.Notifications.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the Notification aggregate.
/// </summary>
public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.Id)
            .HasColumnName("id")
            .HasMaxLength(10)
            .HasConversion(
                id => id.ToString(),
                value => NotificationId.Parse(value));

        builder.Property(n => n.RecipientId)
            .HasColumnName("recipient_id")
            .HasMaxLength(10)
            .HasConversion(
                id => id.ToString(),
                value => UserId.Parse(value))
            .IsRequired();

        builder.Property(n => n.Type)
            .HasColumnName("type")
            .HasMaxLength(40)
            .HasConversion(
                t => t.Name,
                name => NotificationType.FromName(name))
            .IsRequired();

        builder.Property(n => n.Subject)
            .HasColumnName("subject")
            .HasMaxLength(Notification.MaxSubjectLength)
            .IsRequired();

        builder.Property(n => n.Url)
            .HasColumnName("url")
            .HasMaxLength(1024);

        builder.Property(n => n.SourceRef)
            .HasColumnName("source_ref")
            .HasMaxLength(128);

        builder.Property(n => n.IsRead)
            .HasColumnName("is_read")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(n => n.ReadAt)
            .HasColumnName("read_at");

        // Auditing
        builder.Property(n => n.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(n => n.CreatedBy)
            .HasColumnName("created_by")
            .HasMaxLength(100);

        builder.Property(n => n.ModifiedAt)
            .HasColumnName("modified_at");

        builder.Property(n => n.ModifiedBy)
            .HasColumnName("modified_by")
            .HasMaxLength(100);

        builder.Property(n => n.IsDeleted)
            .HasColumnName("is_deleted")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(n => n.DeletedAt)
            .HasColumnName("deleted_at");

        builder.Property(n => n.DeletedBy)
            .HasColumnName("deleted_by")
            .HasMaxLength(100);

        builder.HasQueryFilter(n => !n.IsDeleted);

        // Most queries are: recipient's inbox sorted newest-first, optionally
        // filtered to unread.
        builder.HasIndex(n => new { n.RecipientId, n.CreatedAt });
        builder.HasIndex(n => new { n.RecipientId, n.IsRead });

        builder.Ignore(n => n.DomainEvents);
    }
}
