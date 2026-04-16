using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Traces.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Domain.Auditing;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace GeneFlow.ApiNet2.Domain.Traces.Entities;

/// <summary>
/// Represents an annotation on a trace sequence (e.g., region, feature, point).
/// </summary>
public sealed partial class TraceAnnotation : AuditableEntity<Guid>
{
    public const int MaxLabelLength = 100;
    public const int MaxDescriptionLength = 500;
    public const int MaxColorLength = 20;
    private static readonly Regex HexColorRegex = GenerateHexColorRegex();

    /// <summary>
    /// Type of annotation (Region, Point, Feature, Custom).
    /// </summary>
    public AnnotationType Type { get; private set; } = null!;

    /// <summary>
    /// Label/name for the annotation.
    /// </summary>
    public string Label { get; private set; } = null!;

    /// <summary>
    /// Optional description.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Start position in the sequence (0-based, inclusive).
    /// </summary>
    public int StartPosition { get; private set; }

    /// <summary>
    /// End position in the sequence (0-based, inclusive).
    /// For point annotations, this equals StartPosition.
    /// </summary>
    public int EndPosition { get; private set; }

    /// <summary>
    /// DNA strand (Plus, Minus, None).
    /// </summary>
    public AnnotationStrand Strand { get; private set; } = null!;

    /// <summary>
    /// Color for visual display (hex format, e.g., #FF5733).
    /// </summary>
    public string Color { get; private set; } = null!;

    /// <summary>
    /// Whether this annotation is shared with all study members.
    /// </summary>
    public bool IsShared { get; private set; }

    /// <summary>
    /// Additional metadata as JSON.
    /// </summary>
    public JsonDocument? Metadata { get; private set; }

    private TraceAnnotation() : base() { }

    private TraceAnnotation(
        Guid id,
        AnnotationType type,
        string label,
        string? description,
        int startPosition,
        int endPosition,
        AnnotationStrand strand,
        string color,
        bool isShared,
        JsonDocument? metadata,
        UserId createdBy) : base(id)
    {
        Type = type;
        Label = label;
        Description = description;
        StartPosition = startPosition;
        EndPosition = endPosition;
        Strand = strand;
        Color = color;
        IsShared = isShared;
        Metadata = metadata;

        SetCreationAudit(DateTime.UtcNow, createdBy.Value.ToString());
    }

    internal static Result<TraceAnnotation> Create(
        AnnotationType type,
        string label,
        string? description,
        int startPosition,
        int endPosition,
        AnnotationStrand strand,
        string color,
        bool isShared,
        JsonDocument? metadata,
        UserId createdBy,
        int sequenceLength)
    {
        if (string.IsNullOrWhiteSpace(label))
            return Result.Failure<TraceAnnotation>(TraceErrors.AnnotationLabelRequired);

        var trimmedLabel = label.Trim();
        if (trimmedLabel.Length > MaxLabelLength)
            return Result.Failure<TraceAnnotation>(TraceErrors.AnnotationLabelTooLong(MaxLabelLength));

        if (description?.Length > MaxDescriptionLength)
            return Result.Failure<TraceAnnotation>(TraceErrors.AnnotationDescriptionTooLong(MaxDescriptionLength));

        if (startPosition < 0 || startPosition >= sequenceLength)
            return Result.Failure<TraceAnnotation>(TraceErrors.AnnotationOutOfBounds);

        if (endPosition < startPosition)
            return Result.Failure<TraceAnnotation>(TraceErrors.InvalidAnnotationRange);

        if (endPosition >= sequenceLength)
            return Result.Failure<TraceAnnotation>(TraceErrors.AnnotationOutOfBounds);

        // For point annotations, start and end must be the same
        if (!type.IsRange && startPosition != endPosition)
            endPosition = startPosition;

        if (string.IsNullOrWhiteSpace(color))
            color = "#808080"; // Default gray

        var normalizedColor = color.Trim();
        if (!IsValidColor(normalizedColor))
            return Result.Failure<TraceAnnotation>(TraceErrors.InvalidColor);

        return new TraceAnnotation(
            Guid.NewGuid(),
            type,
            trimmedLabel,
            description?.Trim(),
            startPosition,
            endPosition,
            strand,
            normalizedColor,
            isShared,
            metadata,
            createdBy);
    }

    internal Result Update(
        string label,
        string? description,
        int startPosition,
        int endPosition,
        AnnotationStrand strand,
        string color,
        bool isShared,
        JsonDocument? metadata,
        UserId updatedBy,
        int sequenceLength)
    {
        if (string.IsNullOrWhiteSpace(label))
            return Result.Failure(TraceErrors.AnnotationLabelRequired);

        var trimmedLabel = label.Trim();
        if (trimmedLabel.Length > MaxLabelLength)
            return Result.Failure(TraceErrors.AnnotationLabelTooLong(MaxLabelLength));

        if (description?.Length > MaxDescriptionLength)
            return Result.Failure(TraceErrors.AnnotationDescriptionTooLong(MaxDescriptionLength));

        if (startPosition < 0 || startPosition >= sequenceLength)
            return Result.Failure(TraceErrors.AnnotationOutOfBounds);

        if (endPosition < startPosition)
            return Result.Failure(TraceErrors.InvalidAnnotationRange);

        if (endPosition >= sequenceLength)
            return Result.Failure(TraceErrors.AnnotationOutOfBounds);

        if (!Type.IsRange && startPosition != endPosition)
            endPosition = startPosition;

        if (string.IsNullOrWhiteSpace(color))
            color = "#808080";

        var normalizedColor = color.Trim();
        if (!IsValidColor(normalizedColor))
            return Result.Failure(TraceErrors.InvalidColor);

        Label = trimmedLabel;
        Description = description?.Trim();
        StartPosition = startPosition;
        EndPosition = endPosition;
        Strand = strand;
        Color = normalizedColor;
        IsShared = isShared;
        Metadata?.Dispose();
        Metadata = metadata;

        SetModificationAudit(DateTime.UtcNow, updatedBy.Value.ToString());

        return Result.Success();
    }

    /// <summary>
    /// Gets the length of the annotated region.
    /// </summary>
    public int Length => EndPosition - StartPosition + 1;

    private static bool IsValidColor(string color)
    {
        return HexColorRegex.IsMatch(color);
    }

    [GeneratedRegex(@"^#[0-9A-Fa-f]{6}$", RegexOptions.Compiled)]
    private static partial Regex GenerateHexColorRegex();
}
