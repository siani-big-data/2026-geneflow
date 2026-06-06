using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Studies.Enumerations;
using GeneFlow.ApiNet2.Domain.Studies.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GeneFlow.ApiNet2.Infrastructure.Studies.Persistence.Configurations;

/// <summary>
/// EF Core configuration for Study aggregate.
/// </summary>
public sealed class StudyConfiguration : IEntityTypeConfiguration<Study>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Study> builder)
    {
        builder.ToTable("studies");

        builder.HasKey(s => s.Id);

        // Configure StudyId
        builder.Property(s => s.Id)
            .HasColumnName("id")
            .HasMaxLength(10)
            .HasConversion(
                id => id.ToString(),
                value => StudyId.Parse(value));

        // Configure OwnerId (reference to User aggregate)
        builder.Property(s => s.OwnerId)
            .HasColumnName("owner_id")
            .HasMaxLength(10)
            .HasConversion(
                id => id.ToString(),
                value => UserId.Parse(value))
            .IsRequired();

        builder.HasIndex(s => s.OwnerId);

        // StudyTitle (owned value object)
        builder.OwnsOne(s => s.Title, title =>
        {
            title.Property(t => t.Value)
                .HasColumnName("title")
                .HasMaxLength(StudyTitle.MaxLength)
                .IsRequired();
        });

        // StudyDescription (owned value object)
        builder.OwnsOne(s => s.Description, description =>
        {
            description.Property(d => d.Value)
                .HasColumnName("description")
                .HasMaxLength(StudyDescription.MaxLength);
        });

        // ResearchField (smart enumeration stored as string)
        builder.Property(s => s.ResearchField)
            .HasColumnName("research_field")
            .HasMaxLength(50)
            .HasConversion(
                field => field.Name,
                name => ResearchField.FromName(name))
            .IsRequired();

        builder.HasIndex(s => s.ResearchField);

        // StudyStatus (smart enumeration stored as string)
        builder.Property(s => s.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .HasConversion(
                status => status.Name,
                name => StudyStatus.FromName(name))
            .IsRequired();

        builder.HasIndex(s => s.Status);

        // StudySettings (owned value object)
        builder.OwnsOne(s => s.Settings, settings =>
        {
            settings.Property(st => st.AllowPublicComments)
                .HasColumnName("allow_public_comments")
                .HasDefaultValue(true);

            settings.Property(st => st.AllowDataDownload)
                .HasColumnName("allow_data_download")
                .HasDefaultValue(false);

            settings.Property(st => st.RequireApprovalToJoin)
                .HasColumnName("requiREDACTED")
                .HasDefaultValue(true);
        });

        // StudyMetrics (owned value object)
        builder.OwnsOne(s => s.Metrics, metrics =>
        {
            metrics.Property(m => m.ViewsCount)
                .HasColumnName("views_count")
                .HasDefaultValue(0);

            metrics.Property(m => m.StarsCount)
                .HasColumnName("stars_count")
                .HasDefaultValue(0);
        });

        // Institution
        builder.Property(s => s.Institution)
            .HasColumnName("institution")
            .HasMaxLength(Study.MaxInstitutionLength);

        // PrincipalInvestigator
        builder.Property(s => s.PrincipalInvestigator)
            .HasColumnName("principal_investigator")
            .HasMaxLength(Study.MaxPrincipalInvestigatorLength);

        // README markdown (nullable, stored as text without length cap so that the
        // domain MaxReadmeLength check is the single source of truth and PostgreSQL
        // doesn't need a varchar TOAST roundtrip on read).
        builder.Property(s => s.ReadmeMarkdown)
            .HasColumnName("readme_markdown")
            .HasColumnType("text");

        // IsFeatured
        builder.Property(s => s.IsFeatured)
            .HasColumnName("is_featured")
            .HasDefaultValue(false);

        builder.HasIndex(s => s.IsFeatured);

        // Tags (stored as string array in PostgreSQL)
        // Use backing field to avoid IReadOnlyList<string> converter issues with InMemory provider
        builder.Property<List<string>>("_tags")
            .HasColumnName("tags")
            .HasColumnType("text[]")
            .HasConversion(
                tags => tags.ToArray(),
                array => array.ToList())
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(s => s.Tags);

        // Members (owned collection)
        builder.OwnsMany(s => s.Members, member =>
        {
            member.ToTable("study_members");

            // Guid PK is generated in the domain (StudyMember.Create => Guid.NewGuid()).
            // ValueGeneratedNever() prevents EF from treating new owned entries with
            // a pre-set Guid as "loaded from DB" (which would mark them Modified
            // instead of Added and trigger DbUpdateConcurrencyException on insert).
            member.Property<Guid>("Id")
                .HasColumnName("id")
                .ValueGeneratedNever();
            member.HasKey("Id");

            member.WithOwner().HasForeignKey("study_id");

            // Mark the shadow FK as required so EF generates DELETE when a member is
            // removed from the collection, instead of trying to orphan it via
            // UPDATE study_id = NULL (which violates the NOT NULL DB constraint).
            // Use the non-generic Property(name) overload to configure the existing
            // shadow property without re-declaring its CLR type (must match the
            // principal key's StudyId type, not string).
            member.Property("study_id").IsRequired();

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
                    role => role.Name,
                    name => StudyRole.FromName(name))
                .IsRequired();

            member.Property(m => m.JoinedAt)
                .HasColumnName("joined_at")
                .IsRequired();

            member.Property(m => m.InvitedBy)
                .HasColumnName("invited_by")
                .HasMaxLength(10)
                .HasConversion(
                    id => id != null ? id.ToString() : null,
                    value => value != null ? UserId.Parse(value) : null);

            member.HasIndex("study_id", "UserId").IsUnique();
        });

        // Papers (owned collection)
        builder.OwnsMany(s => s.Papers, paper =>
        {
            paper.ToTable("study_papers");

            paper.Property(p => p.Id)
                .HasColumnName("id")
                .HasMaxLength(10)
                .HasConversion(
                    id => id.ToString(),
                    value => StudyPaperId.Parse(value));

            paper.HasKey(p => p.Id);

            paper.WithOwner().HasForeignKey("study_id");

            paper.Property(p => p.Title)
                .HasColumnName("title")
                .HasMaxLength(500)
                .IsRequired();

            paper.Property(p => p.Authors)
                .HasColumnName("authors")
                .HasMaxLength(1000);

            paper.Property(p => p.Doi)
                .HasColumnName("doi")
                .HasMaxLength(100);

            paper.Property(p => p.Abstract)
                .HasColumnName("abstract")
                .HasMaxLength(5000);

            paper.Property(p => p.Journal)
                .HasColumnName("journal")
                .HasMaxLength(200);

            paper.Property(p => p.PublicationYear)
                .HasColumnName("publication_year");

            paper.Property(p => p.FileId)
                .HasColumnName("file_id")
                .HasMaxLength(100);

            paper.Property(p => p.FileName)
                .HasColumnName("file_name")
                .HasMaxLength(255);

            paper.Property(p => p.FileSizeBytes)
                .HasColumnName("file_size_bytes");

            // Auditing fields
            paper.Property(p => p.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();

            paper.Property(p => p.CreatedBy)
                .HasColumnName("created_by")
                .HasMaxLength(100);

            paper.Property(p => p.ModifiedAt)
                .HasColumnName("modified_at");

            paper.Property(p => p.ModifiedBy)
                .HasColumnName("modified_by")
                .HasMaxLength(100);

            paper.Property(p => p.IsDeleted)
                .HasColumnName("is_deleted")
                .HasDefaultValue(false);

            paper.Property(p => p.DeletedAt)
                .HasColumnName("deleted_at");

            paper.Property(p => p.DeletedBy)
                .HasColumnName("deleted_by")
                .HasMaxLength(100);

            paper.Ignore(p => p.HasFile);
        });

        // Auditing fields
        builder.Property(s => s.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(s => s.CreatedBy)
            .HasColumnName("created_by")
            .HasMaxLength(100);

        builder.Property(s => s.ModifiedAt)
            .HasColumnName("modified_at");

        builder.Property(s => s.ModifiedBy)
            .HasColumnName("modified_by")
            .HasMaxLength(100);

        builder.Property(s => s.IsDeleted)
            .HasColumnName("is_deleted")
            .IsRequired();

        builder.Property(s => s.DeletedAt)
            .HasColumnName("deleted_at");

        builder.Property(s => s.DeletedBy)
            .HasColumnName("deleted_by")
            .HasMaxLength(100);

        // Soft delete filter
        builder.HasQueryFilter(s => !s.IsDeleted);

        // Ignore computed properties
        builder.Ignore(s => s.IsPublic);
        builder.Ignore(s => s.MemberCount);
        builder.Ignore(s => s.PaperCount);
        builder.Ignore(s => s.TagCount);
    }
}
