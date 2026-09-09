using GeneFlow.ApiNet2.Domain.Activity;
using GeneFlow.ApiNet2.Domain.Activity.Enumerations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GeneFlow.ApiNet2.Infrastructure.Activity.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the <see cref="ActivityEvent"/> entity.
/// </summary>
public sealed class ActivityEventConfiguration : IEntityTypeConfiguration<ActivityEvent>
{
    private const int ActorIdMaxLength = 32;
    private const int VerbMaxLength = 32;
    private const int ObjectTypeMaxLength = 32;
    private const int ObjectIdMaxLength = 64;
    private const int StudyIdMaxLength = 32;
    private const int VisibilityMaxLength = 16;
    private const int SourceEventTypeMaxLength = 200;
    private const int SourceMessageIdMaxLength = 64;

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ActivityEvent> builder)
    {
        builder.ToTable("activity_events");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasConversion(
                id => id.Value,
                value => ActivityEventId.From(value));

        builder.Property(e => e.ActorUserId)
            .HasColumnName("actor_user_id")
            .HasMaxLength(ActorIdMaxLength);

        builder.Property(e => e.Verb)
            .HasColumnName("verb")
            .HasMaxLength(VerbMaxLength)
            .IsRequired()
            .HasConversion(
                v => v.Name,
                name => ActivityVerb.FromName(name));

        builder.Property(e => e.ObjectType)
            .HasColumnName("object_type")
            .HasMaxLength(ObjectTypeMaxLength)
            .IsRequired()
            .HasConversion(
                o => o.Name,
                name => ActivityObjectType.FromName(name));

        builder.Property(e => e.ObjectId)
            .HasColumnName("object_id")
            .HasMaxLength(ObjectIdMaxLength)
            .IsRequired();

        builder.Property(e => e.StudyId)
            .HasColumnName("study_id")
            .HasMaxLength(StudyIdMaxLength);

        builder.Property(e => e.OccurredAt)
            .HasColumnName("occurred_at")
            .IsRequired();

        builder.Property(e => e.Visibility)
            .HasColumnName("visibility")
            .HasMaxLength(VisibilityMaxLength)
            .IsRequired()
            .HasConversion(
                v => v.Name,
                name => ActivityVisibility.FromName(name));

        builder.Property(e => e.PayloadJson)
            .HasColumnName("payload_json")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(e => e.SourceEventType)
            .HasColumnName("source_event_type")
            .HasMaxLength(SourceEventTypeMaxLength);

        builder.Property(e => e.SourceMessageId)
            .HasColumnName("source_message_id")
            .HasMaxLength(SourceMessageIdMaxLength);

        // Keyset pagination over (occurred_at DESC, id DESC) is the global feed.
        builder.HasIndex(e => new { e.OccurredAt, e.Id })
            .HasDatabaseName("ix_activity_events_occurred_at_id")
            .IsDescending(true, true);

        // Personal feed (actor) and audit log.
        builder.HasIndex(e => new { e.ActorUserId, e.OccurredAt, e.Id })
            .HasDatabaseName("ix_activity_events_actor_occurred_at_id")
            .IsDescending(false, true, true);

        // Study timeline.
        builder.HasIndex(e => new { e.StudyId, e.OccurredAt, e.Id })
            .HasDatabaseName("ix_activity_events_study_occurred_at_id")
            .IsDescending(false, true, true);

        // Idempotency: exactly one row per source envelope.
        builder.HasIndex(e => e.SourceMessageId)
            .HasDatabaseName("ux_activity_events_source_message_id")
            .IsUnique()
            .HasFilter("source_message_id IS NOT NULL");
    }
}
