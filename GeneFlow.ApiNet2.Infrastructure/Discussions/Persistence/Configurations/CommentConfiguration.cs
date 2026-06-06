using GeneFlow.ApiNet2.Domain.Discussions.Entities;
using GeneFlow.ApiNet2.Domain.Discussions.Enumerations;
using GeneFlow.ApiNet2.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GeneFlow.ApiNet2.Infrastructure.Discussions.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the Comment aggregate. Comments are
/// polymorphic via (ParentType, ParentId) — there is no FK to Discussion
/// at the relational level so the same table can serve future parent
/// contexts (Annotation, Trace) without migration.
/// </summary>
public sealed class CommentConfiguration : IEntityTypeConfiguration<Comment>
{
    public void Configure(EntityTypeBuilder<Comment> builder)
    {
        builder.ToTable("comments");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(c => c.ParentType)
            .HasColumnName("parent_type")
            .HasMaxLength(20)
            .HasConversion(
                pt => pt.Name,
                name => CommentParentType.FromName(name))
            .IsRequired();

        builder.Property(c => c.ParentId)
            .HasColumnName("parent_id")
            .HasMaxLength(40)
            .IsRequired();

        // Browse comments for a parent.
        builder.HasIndex(c => new { c.ParentType, c.ParentId, c.CreatedAt });

        builder.Property(c => c.AuthorId)
            .HasColumnName("author_id")
            .HasMaxLength(10)
            .HasConversion(
                id => id.ToString(),
                value => UserId.Parse(value))
            .IsRequired();

        builder.HasIndex(c => c.AuthorId);

        builder.Property(c => c.BodyMarkdown)
            .HasColumnName("body_markdown")
            .HasMaxLength(Comment.MaxBodyLength)
            .IsRequired();

        builder.Property(c => c.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(c => c.EditedAt)
            .HasColumnName("edited_at");

        builder.Property(c => c.IsDeleted)
            .HasColumnName("is_deleted")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(c => c.DeletedAt)
            .HasColumnName("deleted_at");

        builder.Ignore(c => c.DomainEvents);
    }
}
