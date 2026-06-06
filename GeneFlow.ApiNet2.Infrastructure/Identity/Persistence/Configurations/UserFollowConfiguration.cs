using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Identity.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GeneFlow.ApiNet2.Infrastructure.Identity.Persistence.Configurations;

/// <summary>
/// EF Core configuration for UserFollow (social graph edge).
/// </summary>
public sealed class UserFollowConfiguration : IEntityTypeConfiguration<UserFollow>
{
    public void Configure(EntityTypeBuilder<UserFollow> builder)
    {
        builder.ToTable("user_follows");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.Id)
            .HasColumnName("id");

        builder.Property(f => f.FollowerId)
            .HasColumnName("follower_id")
            .HasMaxLength(10)
            .HasConversion(
                id => id.ToString(),
                value => UserId.Parse(value))
            .IsRequired();

        builder.HasIndex(f => f.FollowerId);

        builder.Property(f => f.FolloweeId)
            .HasColumnName("followee_id")
            .HasMaxLength(10)
            .HasConversion(
                id => id.ToString(),
                value => UserId.Parse(value))
            .IsRequired();

        builder.HasIndex(f => f.FolloweeId);

        // Unique composite index (logical PK): one edge per (follower, followee)
        builder.HasIndex(f => new { f.FollowerId, f.FolloweeId }).IsUnique();

        builder.Property(f => f.FollowedAt)
            .HasColumnName("followed_at")
            .IsRequired();
    }
}
