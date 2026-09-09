using System.Text.Json;
using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Identity.Entities;
using GeneFlow.ApiNet2.Domain.Identity.Enumerations;
using GeneFlow.ApiNet2.Domain.Identity.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace GeneFlow.ApiNet2.Infrastructure.Identity.Persistence.Configurations;

/// <summary>
/// EF Core configuration for User aggregate.
/// </summary>
public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id)
            .HasColumnName("id")
            .HasMaxLength(10)
            .HasConversion(
                id => id.ToString(),
                value => UserId.Parse(value));

        builder.Property(u => u.Email)
            .HasColumnName("email")
            .HasMaxLength(Email.MaxLength)
            .HasConversion(
                email => email.Value,
                value => Email.Create(value).Value)
            .IsRequired();

        builder.HasIndex(u => u.Email).IsUnique();

        builder.Property(u => u.Username)
            .HasColumnName("username")
            .HasMaxLength(Username.MaxLength)
            .HasConversion(
                username => username.Value,
                value => Username.Create(value).Value)
            .IsRequired();

        builder.HasIndex(u => u.Username).IsUnique();

        builder.Property(u => u.PasswordHash)
            .HasColumnName("password_hash")
            .HasMaxLength(256)
            .HasConversion(
                hash => hash.Value,
                value => PasswordHash.Create(value).Value)
            .IsRequired();

        builder.Property(u => u.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        // Email Verification (owned type)
        builder.OwnsOne(u => u.EmailVerification, ev =>
        {
            ev.Property(e => e.IsVerified)
                .HasColumnName("email_verified")
                .IsRequired();

            ev.Property(e => e.Token)
                .HasColumnName("email_verification_token")
                .HasMaxLength(128);

            ev.Property(e => e.TokenExpiry)
                .HasColumnName("email_verification_token_expiry");
        });

        // Password Reset (owned type)
        builder.OwnsOne(u => u.PasswordReset, pr =>
        {
            pr.Property(p => p.Token)
                .HasColumnName("password_reset_token")
                .HasMaxLength(128);

            pr.Property(p => p.TokenExpiry)
                .HasColumnName("password_reset_token_expiry");
        });

        // Account Lockout (owned type)
        builder.OwnsOne(u => u.Lockout, lo =>
        {
            lo.Property(l => l.FailedAttempts)
                .HasColumnName("failed_login_attempts")
                .IsRequired();

            lo.Property(l => l.LockoutEnd)
                .HasColumnName("lockout_end");
        });

        // Two Factor Auth (owned type with nested collection)
        builder.OwnsOne(u => u.TwoFactorAuth, tfa =>
        {
            tfa.Property(t => t.IsEnabled)
                .HasColumnName("two_factor_enabled")
                .IsRequired();

            // TOTP Secret (owned type within TwoFactorAuth)
            tfa.OwnsOne(t => t.TotpSecret, ts =>
            {
                ts.Property(s => s.EncryptedSecret)
                    .HasColumnName("totp_secret")
                    .HasMaxLength(512);

                ts.Property(s => s.CreatedAt)
                    .HasColumnName("totp_secret_created_at");
            });

            tfa.OwnsMany(t => t.Codes, code =>
            {
                code.ToTable("two_factor_codes");

                code.WithOwner().HasForeignKey("UserId");

                code.Property<Guid>("Id")
                    .HasColumnName("id");

                code.HasKey("Id");

                code.Property(c => c.Code)
                    .HasColumnName("code")
                    .HasMaxLength(6)
                    .IsRequired();

                code.Property(c => c.CreatedAt)
                    .HasColumnName("created_at")
                    .IsRequired();

                code.Property(c => c.ExpiresAt)
                    .HasColumnName("expires_at")
                    .IsRequired();

                code.Property(c => c.IsUsed)
                    .HasColumnName("is_used")
                    .IsRequired();

                code.Property(c => c.UsedAt)
                    .HasColumnName("used_at");
            });
        });

        // Roles (collection of enumerations as JSON)
        var rolesConverter = new ValueConverter<List<Role>, string>(
            roles => JsonSerializer.Serialize(roles.Select(r => r.Name).ToList(), (JsonSerializerOptions?)null),
            json => JsonSerializer.Deserialize<List<string>>(json, (JsonSerializerOptions?)null)!
                .Select(name => Role.FromName(name)!)
                .ToList());

        var rolesComparer = new ValueComparer<List<Role>>(
            (c1, c2) => c1 != null && c2 != null && c1.Select(r => r.Name).SequenceEqual(c2.Select(r => r.Name)),
            c => c.Aggregate(0, (a, r) => HashCode.Combine(a, r.Name.GetHashCode())),
            c => c.Select(r => Role.FromName(r.Name)!).ToList());

        builder.Property<List<Role>>("_roles")
            .HasColumnName("roles")
            .HasColumnType("jsonb")
            .HasConversion(rolesConverter)
            .Metadata.SetValueComparer(rolesComparer);

        // Ignore the Roles navigation property - we use the backing field _roles
        builder.Ignore(u => u.Roles);

        // Refresh Tokens (owned collection)
        builder.OwnsMany<RefreshToken>("_refreshTokens", rt =>
        {
            rt.ToTable("refresh_tokens");

            rt.WithOwner().HasForeignKey("UserId");

            rt.Property(r => r.Token)
                .HasColumnName("token")
                .HasMaxLength(256)
                .IsRequired();

            rt.HasKey(r => r.Token);

            rt.Property(r => r.ExpiresAt)
                .HasColumnName("expires_at")
                .IsRequired();

            rt.Property(r => r.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();

            rt.Property(r => r.IsRevoked)
                .HasColumnName("is_revoked")
                .IsRequired();

            rt.Property(r => r.RevokedAt)
                .HasColumnName("revoked_at");

            rt.Property(r => r.ReplacedByToken)
                .HasColumnName("replaced_by_token")
                .HasMaxLength(256);

            rt.HasIndex(r => r.Token);
        });

        // External Logins (owned collection)
        builder.OwnsMany<ExternalLogin>("_externalLogins", el =>
        {
            el.ToTable("external_logins");

            el.WithOwner().HasForeignKey("UserId");

            el.Property<Guid>("Id")
                .HasColumnName("id");

            el.HasKey("Id");

            el.Property(e => e.Provider)
                .HasColumnName("provider")
                .HasMaxLength(50)
                .HasConversion(
                    provider => provider.Name,
                    name => ExternalProvider.FromName(name)!)
                .IsRequired();

            el.Property(e => e.ProviderKey)
                .HasColumnName("provider_key")
                .HasMaxLength(256)
                .IsRequired();

            el.Property(e => e.ProviderDisplayName)
                .HasColumnName("provider_display_name")
                .HasMaxLength(256);

            el.Property(e => e.LinkedAt)
                .HasColumnName("linked_at")
                .IsRequired();

            el.HasIndex(e => new { e.Provider, e.ProviderKey }).IsUnique();
        });

        // Auditing fields
        builder.Property(u => u.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(u => u.CreatedBy)
            .HasColumnName("created_by")
            .HasMaxLength(100);

        builder.Property(u => u.ModifiedAt)
            .HasColumnName("modified_at");

        builder.Property(u => u.ModifiedBy)
            .HasColumnName("modified_by")
            .HasMaxLength(100);

        builder.Property(u => u.IsDeleted)
            .HasColumnName("is_deleted")
            .IsRequired();

        builder.Property(u => u.DeletedAt)
            .HasColumnName("deleted_at");

        builder.Property(u => u.DeletedBy)
            .HasColumnName("deleted_by")
            .HasMaxLength(100);

        // Soft delete filter
        builder.HasQueryFilter(u => !u.IsDeleted);

        // Ignore navigation properties - we use backing fields for owned collections
        builder.Ignore(u => u.RefreshTokens);
        builder.Ignore(u => u.ExternalLogins);
        builder.Ignore(u => u.TwoFactorCodes);

        // Ignore computed properties
        builder.Ignore(u => u.TotpSecret);
        builder.Ignore(u => u.IsTotpConfigured);
    }
}
