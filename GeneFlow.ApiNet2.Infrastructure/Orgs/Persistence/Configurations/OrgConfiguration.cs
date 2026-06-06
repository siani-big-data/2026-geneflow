using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Orgs.Enumerations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrgAggregate = GeneFlow.ApiNet2.Domain.Orgs.Org;
using OrgIdType = GeneFlow.ApiNet2.Domain.Orgs.OrgId;

namespace GeneFlow.ApiNet2.Infrastructure.Orgs.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the Org aggregate. Tables live in schema
/// <c>orgs</c>: <c>orgs</c> (root) and <c>org_members</c> (owned collection).
/// </summary>
public sealed class OrgConfiguration : IEntityTypeConfiguration<OrgAggregate>
{
    public void Configure(EntityTypeBuilder<OrgAggregate> builder)
    {
        builder.ToTable("orgs");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Id)
            .HasColumnName("id")
            .HasMaxLength(10)
            .HasConversion(
                id => id.ToString(),
                value => OrgIdType.Parse(value));

        builder.Property(o => o.Handle)
            .HasColumnName("handle")
            .HasMaxLength(39)
            .IsRequired();

        builder.HasIndex(o => o.Handle).IsUnique();

        builder.Property(o => o.Name)
            .HasColumnName("name")
            .HasMaxLength(OrgAggregate.MaxNameLength)
            .IsRequired();

        builder.Property(o => o.Description)
            .HasColumnName("description")
            .HasMaxLength(OrgAggregate.MaxDescriptionLength);

        builder.Property(o => o.AvatarUrl)
            .HasColumnName("avatar_url")
            .HasMaxLength(OrgAggregate.MaxUrlLength);

        builder.Property(o => o.WebsiteUrl)
            .HasColumnName("website_url")
            .HasMaxLength(OrgAggregate.MaxUrlLength);

        builder.Property(o => o.Location)
            .HasColumnName("location")
            .HasMaxLength(OrgAggregate.MaxLocationLength);

        builder.Property(o => o.Visibility)
            .HasColumnName("visibility")
            .HasMaxLength(20)
            .HasConversion(
                v => v.Name,
                name => OrgVisibility.FromName(name)!)
            .IsRequired();

        // Owned collection: members.
        builder.OwnsMany(o => o.Members, member =>
        {
            member.ToTable("org_members");

            member.Property<Guid>("Id")
                .HasColumnName("id")
                .ValueGeneratedNever();
            member.HasKey("Id");

            member.WithOwner().HasForeignKey("org_id");
            member.Property("org_id").IsRequired();

            member.Property(m => m.OrgId)
                .HasColumnName("org_id_value")
                .HasMaxLength(10)
                .HasConversion(
                    id => id.ToString(),
                    value => OrgIdType.Parse(value))
                .IsRequired();

            member.Property(m => m.UserId)
                .HasColumnName("user_id")
                .HasMaxLength(10)
                .HasConversion(
                    id => id.ToString(),
                    value => UserId.Parse(value))
                .IsRequired();

            member.Property(m => m.Role)
                .HasColumnName("role")
                .HasMaxLength(20)
                .HasConversion(
                    r => r.Name,
                    name => OrgRole.FromName(name)!)
                .IsRequired();

            member.Property(m => m.JoinedAt)
                .HasColumnName("joined_at")
                .IsRequired();

            member.HasIndex("org_id", "UserId").IsUnique();
        });

        // Auditing fields
        builder.Property(o => o.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(o => o.CreatedBy)
            .HasColumnName("created_by")
            .HasMaxLength(100);

        builder.Property(o => o.ModifiedAt)
            .HasColumnName("modified_at");

        builder.Property(o => o.ModifiedBy)
            .HasColumnName("modified_by")
            .HasMaxLength(100);

        builder.Property(o => o.IsDeleted)
            .HasColumnName("is_deleted")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(o => o.DeletedAt)
            .HasColumnName("deleted_at");

        builder.Property(o => o.DeletedBy)
            .HasColumnName("deleted_by")
            .HasMaxLength(100);

        builder.HasQueryFilter(o => !o.IsDeleted);

        builder.Ignore(o => o.DomainEvents);
    }
}
