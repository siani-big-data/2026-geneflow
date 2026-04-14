using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Traces;
using GeneFlow.ApiNet2.Domain.Traces.Enumerations;
using GeneFlow.ApiNet2.Domain.Traces.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GeneFlow.ApiNet2.Infrastructure.Traces.Persistence.Configurations;

/// <summary>
/// EF Core configuration for Trace aggregate.
/// </summary>
public sealed class TraceConfiguration : IEntityTypeConfiguration<Trace>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Trace> builder)
    {
        builder.ToTable("traces");

        builder.HasKey(t => t.Id);

        // Configure TraceId (UUID)
        builder.Property(t => t.Id)
            .HasColumnName("id")
            .HasConversion(
                id => id.Value,
                value => TraceId.From(value));

        // Configure StudyId (reference to Study aggregate)
        builder.Property(t => t.StudyId)
            .HasColumnName("study_id")
            .HasMaxLength(10)
            .HasConversion(
                id => id.ToString(),
                value => StudyId.Parse(value))
            .IsRequired();

        builder.HasIndex(t => t.StudyId);

        // Configure UploadedBy (reference to User aggregate)
        builder.Property(t => t.UploadedBy)
            .HasColumnName("uploaded_by")
            .HasMaxLength(10)
            .HasConversion(
                id => id.ToString(),
                value => UserId.Parse(value))
            .IsRequired();

        // TraceName (owned value object)
        builder.OwnsOne(t => t.Name, name =>
        {
            name.Property(n => n.Value)
                .HasColumnName("name")
                .HasMaxLength(TraceName.MaxLength)
                .IsRequired();
        });

        // TraceDescription (owned value object)
        builder.OwnsOne(t => t.Description, description =>
        {
            description.Property(d => d.Value)
                .HasColumnName("description")
                .HasMaxLength(TraceDescription.MaxLength);
        });

        // TraceFile (owned value object)
        builder.OwnsOne(t => t.File, file =>
        {
            file.Property(f => f.FileName)
                .HasColumnName("file_name")
                .HasMaxLength(TraceFile.MaxFileNameLength)
                .IsRequired();

            file.Property(f => f.ContentType)
                .HasColumnName("content_type")
                .HasMaxLength(TraceFile.MaxContentTypeLength)
                .IsRequired();

            file.Property(f => f.StoragePath)
                .HasColumnName("storage_path")
                .HasMaxLength(TraceFile.MaxStoragePathLength)
                .IsRequired();

            file.Property(f => f.SizeBytes)
                .HasColumnName("size_bytes")
                .IsRequired();

            file.Property(f => f.Checksum)
                .HasColumnName("checksum")
                .HasMaxLength(TraceFile.MaxChecksumLength)
                .IsRequired();
        });

        // TraceFormat (smart enumeration stored as string)
        builder.Property(t => t.Format)
            .HasColumnName("format")
            .HasMaxLength(20)
            .HasConversion(
                format => format.Name,
                name => TraceFormat.FromName(name)!)
            .IsRequired();

        builder.HasIndex(t => t.Format);

        // TraceStatus (smart enumeration stored as string)
        builder.Property(t => t.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .HasConversion(
                status => status.Name,
                name => TraceStatus.FromName(name)!)
            .IsRequired();

        builder.HasIndex(t => t.Status);

        // QualityMetrics (owned value object, nullable)
        builder.OwnsOne(t => t.QualityMetrics, metrics =>
        {
            metrics.Property(m => m.AverageQualityScore)
                .HasColumnName("average_quality_score")
                .HasPrecision(5, 2);

            metrics.Property(m => m.TotalBases)
                .HasColumnName("total_bases");

            metrics.Property(m => m.QualityAboveQ20Percentage)
                .HasColumnName("quality_above_q20_percentage")
                .HasPrecision(5, 2);

            metrics.Property(m => m.QualityAboveQ30Percentage)
                .HasColumnName("quality_above_q30_percentage")
                .HasPrecision(5, 2);

            metrics.Property(m => m.TrimmedLength)
                .HasColumnName("trimmed_length");

            metrics.Property(m => m.GcContentPercentage)
                .HasColumnName("gc_content_percentage")
                .HasPrecision(5, 2);
        });

        // TrimRegion (owned value object, nullable)
        builder.OwnsOne(t => t.TrimRegion, trim =>
        {
            trim.Property(r => r.Start5Prime)
                .HasColumnName("trim_start_5_prime");

            trim.Property(r => r.End5Prime)
                .HasColumnName("trim_end_5_prime");

            trim.Property(r => r.Start3Prime)
                .HasColumnName("trim_start_3_prime");

            trim.Property(r => r.End3Prime)
                .HasColumnName("trim_end_3_prime");

            trim.Property(r => r.Algorithm)
                .HasColumnName("trim_algorithm")
                .HasMaxLength(TrimRegion.MaxAlgorithmLength);

            trim.Property(r => r.TrimmedBy)
                .HasColumnName("trimmed_by")
                .HasMaxLength(100);

            trim.Property(r => r.TrimmedAt)
                .HasColumnName("trimmed_at");
        });

        // HasChromatogramData
        builder.Property(t => t.HasChromatogramData)
            .HasColumnName("has_chromatogram_data")
            .HasDefaultValue(false);

        // FailureReason
        builder.Property(t => t.FailureReason)
            .HasColumnName("failuREDACTED")
            .HasMaxLength(Trace.MaxFailureReasonLength);

        // ProcessedAt
        builder.Property(t => t.ProcessedAt)
            .HasColumnName("processed_at");

        // Edits (owned collection)
        builder.OwnsMany(t => t.Edits, edit =>
        {
            edit.ToTable("sequence_edits");

            edit.Property<Guid>("Id")
                .HasColumnName("id");
            edit.HasKey("Id");

            edit.WithOwner().HasForeignKey("trace_id");

            edit.Property(e => e.EditType)
                .HasColumnName("edit_type")
                .HasMaxLength(20)
                .HasConversion(
                    type => type.Name,
                    name => EditType.FromName(name)!)
                .IsRequired();

            edit.Property(e => e.Position)
                .HasColumnName("position")
                .IsRequired();

            edit.Property(e => e.OriginalBase)
                .HasColumnName("original_base")
                .HasMaxLength(1);

            edit.Property(e => e.NewBase)
                .HasColumnName("new_base")
                .HasMaxLength(1);

            edit.Property(e => e.Reason)
                .HasColumnName("reason")
                .HasMaxLength(500);

            edit.Property(e => e.EditedBy)
                .HasColumnName("edited_by")
                .HasMaxLength(10)
                .HasConversion(
                    id => id.ToString(),
                    value => UserId.Parse(value))
                .IsRequired();

            edit.Property(e => e.EditedAt)
                .HasColumnName("edited_at")
                .IsRequired();

            edit.Property(e => e.IsActive)
                .HasColumnName("is_active")
                .HasDefaultValue(true);

            edit.HasIndex("trace_id", "Position");
        });

        // Annotations (owned collection)
        builder.OwnsMany(t => t.Annotations, annotation =>
        {
            annotation.ToTable("trace_annotations");

            annotation.Property<Guid>("Id")
                .HasColumnName("id");
            annotation.HasKey("Id");

            annotation.WithOwner().HasForeignKey("trace_id");

            annotation.Property(a => a.Type)
                .HasColumnName("type")
                .HasMaxLength(20)
                .HasConversion(
                    type => type.Name,
                    name => AnnotationType.FromName(name)!)
                .IsRequired();

            annotation.Property(a => a.Label)
                .HasColumnName("label")
                .HasMaxLength(100)
                .IsRequired();

            annotation.Property(a => a.Description)
                .HasColumnName("description")
                .HasMaxLength(500);

            annotation.Property(a => a.StartPosition)
                .HasColumnName("start_position")
                .IsRequired();

            annotation.Property(a => a.EndPosition)
                .HasColumnName("end_position")
                .IsRequired();

            annotation.Property(a => a.Strand)
                .HasColumnName("strand")
                .HasMaxLength(10)
                .HasConversion(
                    strand => strand.Name,
                    name => AnnotationStrand.FromName(name)!)
                .IsRequired();

            annotation.Property(a => a.Color)
                .HasColumnName("color")
                .HasMaxLength(20)
                .IsRequired();

            annotation.Property(a => a.IsShared)
                .HasColumnName("is_shared")
                .HasDefaultValue(false);

            annotation.Property(a => a.Metadata)
                .HasColumnName("metadata")
                .HasColumnType("jsonb");

            // Auditing fields for annotations
            annotation.Property(a => a.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();

            annotation.Property(a => a.CreatedBy)
                .HasColumnName("created_by")
                .HasMaxLength(100);

            annotation.Property(a => a.ModifiedAt)
                .HasColumnName("modified_at");

            annotation.Property(a => a.ModifiedBy)
                .HasColumnName("modified_by")
                .HasMaxLength(100);

            annotation.HasIndex("trace_id", "StartPosition", "EndPosition");
            annotation.HasIndex("trace_id", "IsShared");
        });

        // Auditing fields
        builder.Property(t => t.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(t => t.CreatedBy)
            .HasColumnName("created_by")
            .HasMaxLength(100);

        builder.Property(t => t.ModifiedAt)
            .HasColumnName("modified_at");

        builder.Property(t => t.ModifiedBy)
            .HasColumnName("modified_by")
            .HasMaxLength(100);

        builder.Property(t => t.IsDeleted)
            .HasColumnName("is_deleted")
            .HasDefaultValue(false);

        builder.Property(t => t.DeletedAt)
            .HasColumnName("deleted_at");

        builder.Property(t => t.DeletedBy)
            .HasColumnName("deleted_by")
            .HasMaxLength(100);

        // Soft delete filter
        builder.HasQueryFilter(t => !t.IsDeleted);

        // Ignore computed properties
        builder.Ignore(t => t.IsProcessed);
        builder.Ignore(t => t.IsFailed);
        builder.Ignore(t => t.IsArchived);
        builder.Ignore(t => t.CanBeEdited);
        builder.Ignore(t => t.ActiveEditCount);
        builder.Ignore(t => t.AnnotationCount);
        builder.Ignore(t => t.TotalBases);
    }
}
