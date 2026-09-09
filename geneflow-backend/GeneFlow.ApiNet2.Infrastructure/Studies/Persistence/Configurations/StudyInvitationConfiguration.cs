using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Studies.Entities;
using GeneFlow.ApiNet2.Domain.Studies.Enumerations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GeneFlow.ApiNet2.Infrastructure.Studies.Persistence.Configurations;

/// <summary>
/// EF Core configuration for StudyInvitation entity.
/// </summary>
public sealed class StudyInvitationConfiguration : IEntityTypeConfiguration<StudyInvitation>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<StudyInvitation> builder)
    {
        builder.ToTable("study_invitations");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Id)
            .HasColumnName("id")
            .HasMaxLength(10)
            .HasConversion(
                id => id.ToString(),
                value => StudyInvitationId.Parse(value));

        // StudyId
        builder.Property(i => i.StudyId)
            .HasColumnName("study_id")
            .HasMaxLength(10)
            .HasConversion(
                id => id.ToString(),
                value => StudyId.Parse(value))
            .IsRequired();

        builder.HasIndex(i => i.StudyId);

        // Email
        builder.Property(i => i.Email)
            .HasColumnName("email")
            .HasMaxLength(320)
            .IsRequired();

        builder.HasIndex(i => i.Email);
        builder.HasIndex(i => new { i.StudyId, i.Email });

        // Role
        builder.Property(i => i.Role)
            .HasColumnName("role")
            .HasMaxLength(20)
            .HasConversion(
                role => role.Name,
                name => StudyRole.FromName(name))
            .IsRequired();

        // Status
        builder.Property(i => i.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .HasConversion(
                status => status.Name,
                name => InvitationStatus.FromName(name))
            .IsRequired();

        builder.HasIndex(i => i.Status);

        // Token
        builder.Property(i => i.Token)
            .HasColumnName("token")
            .HasMaxLength(64)
            .IsRequired();

        builder.HasIndex(i => i.Token).IsUnique();

        // InvitedBy
        builder.Property(i => i.InvitedBy)
            .HasColumnName("invited_by")
            .HasMaxLength(10)
            .HasConversion(
                id => id.ToString(),
                value => UserId.Parse(value))
            .IsRequired();

        // ExpiresAt
        builder.Property(i => i.ExpiresAt)
            .HasColumnName("expires_at")
            .IsRequired();

        // RespondedAt
        builder.Property(i => i.RespondedAt)
            .HasColumnName("responded_at");

        // Message
        builder.Property(i => i.Message)
            .HasColumnName("message")
            .HasMaxLength(StudyInvitation.MaxMessageLength);

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

        // Ignore computed properties
        builder.Ignore(i => i.IsExpired);
        builder.Ignore(i => i.IsPending);
    }
}
