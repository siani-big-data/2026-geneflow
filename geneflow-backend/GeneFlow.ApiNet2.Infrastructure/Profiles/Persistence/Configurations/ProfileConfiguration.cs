using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Profiles;
using GeneFlow.ApiNet2.Domain.Profiles.Enumerations;
using GeneFlow.ApiNet2.Domain.Profiles.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GeneFlow.ApiNet2.Infrastructure.Profiles.Persistence.Configurations;

/// <summary>
/// EF Core configuration for Profile aggregate.
/// </summary>
public sealed class ProfileConfiguration : IEntityTypeConfiguration<Profile>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Profile> builder)
    {
        builder.ToTable("profiles");

        builder.HasKey(p => p.Id);

        // Configure ProfileId
        builder.Property(p => p.Id)
            .HasColumnName("id")
            .HasMaxLength(10)
            .HasConversion(
                id => id.ToString(),
                value => ProfileId.Parse(value));

        // Configure UserId (reference to User aggregate in Identity context)
        builder.Property(p => p.UserId)
            .HasColumnName("user_id")
            .HasMaxLength(10)
            .HasConversion(
                id => id.ToString(),
                value => UserId.Parse(value))
            .IsRequired();

        builder.HasIndex(p => p.UserId).IsUnique();

        // PersonName (owned value object)
        builder.OwnsOne(p => p.Name, name =>
        {
            name.Property(n => n.FirstName)
                .HasColumnName("first_name")
                .HasMaxLength(PersonName.FirstNameMaxLength)
                .IsRequired();

            name.Property(n => n.LastName)
                .HasColumnName("last_name")
                .HasMaxLength(PersonName.LastNameMaxLength);

            // Ignore computed properties
            name.Ignore(n => n.FullName);
            name.Ignore(n => n.Initials);
        });

        // Bio (owned value object)
        builder.OwnsOne(p => p.Bio, bio =>
        {
            bio.Property(b => b.Value)
                .HasColumnName("bio")
                .HasMaxLength(Bio.MaxLength);
        });

        // Location (owned value object)
        builder.OwnsOne(p => p.Location, location =>
        {
            location.Property(l => l.Value)
                .HasColumnName("location")
                .HasMaxLength(Location.MaxLength);
        });

        // ProfessionalRole (owned value object)
        builder.OwnsOne(p => p.ProfessionalRole, role =>
        {
            role.Property(r => r.Value)
                .HasColumnName("professional_role")
                .HasMaxLength(ProfessionalRole.MaxLength);
        });

        // Institution (owned value object)
        builder.OwnsOne(p => p.Institution, institution =>
        {
            institution.Property(i => i.Name)
                .HasColumnName("institution_name")
                .HasMaxLength(Institution.NameMaxLength);

            institution.Property(i => i.Department)
                .HasColumnName("institution_department")
                .HasMaxLength(Institution.DepartmentMaxLength);

            // Ignore computed property
            institution.Ignore(i => i.DisplayName);
        });

        // ResearchField (smart enumeration stored as string)
        builder.Property(p => p.ResearchField)
            .HasColumnName("research_field")
            .HasMaxLength(50)
            .HasConversion(
                field => field != null ? field.Name : null,
                name => name != null ? ResearchField.FromName(name) : null);

        builder.HasIndex(p => p.ResearchField);

        // ResearchIdentifiers (owned value object)
        builder.OwnsOne(p => p.ResearchIdentifiers, identifiers =>
        {
            identifiers.Property(i => i.OrcidId)
                .HasColumnName("orcid_id")
                .HasMaxLength(19); // Format: 0000-0000-0000-0000

            identifiers.Property(i => i.Website)
                .HasColumnName("website")
                .HasMaxLength(ResearchIdentifiers.WebsiteMaxLength);

            // Ignore computed property
            identifiers.Ignore(i => i.OrcidUrl);
        });

        // ProfilePhoto (owned value object)
        builder.OwnsOne(p => p.Photo, photo =>
        {
            photo.Property(ph => ph.Url)
                .HasColumnName("photo_url")
                .HasMaxLength(ProfilePhoto.UrlMaxLength);

            photo.Property(ph => ph.ThumbnailUrl)
                .HasColumnName("photo_thumbnail_url")
                .HasMaxLength(ProfilePhoto.UrlMaxLength);

            photo.Property(ph => ph.SizeBytes)
                .HasColumnName("photo_size_bytes");

            // Ignore computed property
            photo.Ignore(ph => ph.HasPhoto);
        });

        // Auditing fields
        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(p => p.CreatedBy)
            .HasColumnName("created_by")
            .HasMaxLength(100);

        builder.Property(p => p.ModifiedAt)
            .HasColumnName("modified_at");

        builder.Property(p => p.ModifiedBy)
            .HasColumnName("modified_by")
            .HasMaxLength(100);

        builder.Property(p => p.IsDeleted)
            .HasColumnName("is_deleted")
            .IsRequired();

        builder.Property(p => p.DeletedAt)
            .HasColumnName("deleted_at");

        builder.Property(p => p.DeletedBy)
            .HasColumnName("deleted_by")
            .HasMaxLength(100);

        // Soft delete filter
        builder.HasQueryFilter(p => !p.IsDeleted);

        // Ignore computed properties
        builder.Ignore(p => p.IsComplete);
        builder.Ignore(p => p.FullName);
        builder.Ignore(p => p.Initials);
    }
}
