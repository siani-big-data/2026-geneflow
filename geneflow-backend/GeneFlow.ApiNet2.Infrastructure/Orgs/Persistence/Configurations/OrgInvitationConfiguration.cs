using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Orgs;
using GeneFlow.ApiNet2.Domain.Orgs.Entities;
using GeneFlow.ApiNet2.Domain.Orgs.Enumerations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GeneFlow.ApiNet2.Infrastructure.Orgs.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the OrgInvitation aggregate. Table lives in
/// schema <c>orgs</c> as <c>org_invitations</c>.
/// </summary>
public sealed class OrgInvitationConfiguration : IEntityTypeConfiguration<OrgInvitation>
{
    public void Configure(EntityTypeBuilder<OrgInvitation> builder)
    {
        builder.ToTable("org_invitations");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Id)
            .HasColumnName("id")
            .HasMaxLength(10)
            .HasConversion(
                id => id.ToString(),
                value => OrgInvitationId.Parse(value));

        builder.Property(i => i.OrgId)
            .HasColumnName("org_id")
            .HasMaxLength(10)
            .HasConversion(
                id => id.ToString(),
                value => OrgId.Parse(value))
            .IsRequired();

        builder.Property(i => i.InvitedEmail)
            .HasColumnName("invited_email")
            .HasMaxLength(320)
            .IsRequired();

        builder.Property(i => i.InvitedUserId)
            .HasColumnName("invited_user_id")
            .HasMaxLength(10)
            .HasConversion(
                id => id == null ? null : id.ToString(),
                value => value == null ? null : UserId.Parse(value));

        builder.Property(i => i.Role)
            .HasColumnName("role")
            .HasMaxLength(20)
            .HasConversion(
                r => r.Name,
                name => OrgRole.FromName(name)!)
            .IsRequired();

        builder.Property(i => i.Token)
            .HasColumnName("token")
            .HasMaxLength(64)
            .IsRequired();

        builder.HasIndex(i => i.Token).IsUnique();

        builder.Property(i => i.ExpiresAt)
            .HasColumnName("expires_at")
            .IsRequired();

        builder.Property(i => i.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .HasConversion(
                s => s.Name,
                name => InvitationStatus.FromName(name)!)
            .IsRequired();

        builder.Property(i => i.AcceptedByUserId)
            .HasColumnName("accepted_by_user_id")
            .HasMaxLength(10)
            .HasConversion(
                id => id == null ? null : id.ToString(),
                value => value == null ? null : UserId.Parse(value));

        // Auditing fields
        builder.Property(i => i.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(i => i.CreatedBy)
            .HasColumnName("created_by")
            .HasMaxLength(100);

        builder.Property(i => i.ModifiedAt)
            .HasColumnName("modified_at");

        builder.Property(i => i.ModifiedBy)
            .HasColumnName("modified_by")
            .HasMaxLength(100);

        builder.Property(i => i.IsDeleted)
            .HasColumnName("is_deleted")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(i => i.DeletedAt)
            .HasColumnName("deleted_at");

        builder.Property(i => i.DeletedBy)
            .HasColumnName("deleted_by")
            .HasMaxLength(100);

        builder.HasQueryFilter(i => !i.IsDeleted);

        builder.HasIndex(i => new { i.InvitedEmail, i.Status });
        builder.HasIndex(i => new { i.OrgId, i.Status });

        builder.Ignore(i => i.DomainEvents);
    }
}
