using GeneFlow.ApiNet2.Domain.Discussions.Entities;
using GeneFlow.ApiNet2.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GeneFlow.ApiNet2.Infrastructure.Discussions.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the Reaction aggregate. Uniqueness on
/// (CommentId, UserId, Emoji) is enforced via a unique composite index.
/// </summary>
public sealed class ReactionConfiguration : IEntityTypeConfiguration<Reaction>
{
    public void Configure(EntityTypeBuilder<Reaction> builder)
    {
        builder.ToTable("reactions");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(r => r.CommentId)
            .HasColumnName("comment_id")
            .IsRequired();

        builder.Property(r => r.UserId)
            .HasColumnName("user_id")
            .HasMaxLength(10)
            .HasConversion(
                id => id.ToString(),
                value => UserId.Parse(value))
            .IsRequired();

        builder.Property(r => r.Emoji)
            .HasColumnName("emoji")
            .HasMaxLength(Reaction.MaxEmojiLength)
            .IsRequired();

        builder.Property(r => r.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        // Unique tuple — a user can only register one of each emoji per comment.
        builder.HasIndex(r => new { r.CommentId, r.UserId, r.Emoji })
            .IsUnique();

        // Fast lookup by comment for the reactions summary.
        builder.HasIndex(r => r.CommentId);

        builder.Ignore(r => r.DomainEvents);
    }
}
