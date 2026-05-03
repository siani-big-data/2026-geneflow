using System.Text.Json;
using GeneFlow.ApiNet2.Domain.Pipelines.Enumerations;
using GeneFlow.ApiNet2.SharedKernel.Domain.DDD;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Domain.Pipelines.ValueObjects;

/// <summary>
/// Represents the configuration for a pipeline step.
/// Stores JSON configuration validated against the step type's schema.
/// </summary>
public sealed class StepConfiguration : ValueObject
{
    public const int MaxLength = 4000;

    /// <summary>
    /// The raw JSON configuration.
    /// </summary>
    public string Value { get; }

    private StepConfiguration(string value) => Value = value;

    /// <summary>
    /// Creates a step configuration from a JSON string.
    /// </summary>
    public static Result<StepConfiguration> Create(string? json, StepType stepType)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            // Empty configuration is valid for steps that don't require configuration
            if (stepType.RequiresConfiguration)
                return Result.Failure<StepConfiguration>(PipelineErrors.StepConfigurationRequired(stepType.Name));

            return new StepConfiguration("{}");
        }

        var trimmed = json.Trim();

        if (trimmed.Length > MaxLength)
            return Result.Failure<StepConfiguration>(PipelineErrors.StepConfigurationTooLong(MaxLength));

        // Validate JSON syntax
        try
        {
            using var doc = JsonDocument.Parse(trimmed);

            // Basic validation based on step type
            var validationResult = ValidateForStepType(doc, stepType);
            if (validationResult.IsFailure)
                return Result.Failure<StepConfiguration>(validationResult.Error);
        }
        catch (JsonException)
        {
            return Result.Failure<StepConfiguration>(PipelineErrors.InvalidStepConfiguration);
        }

        return new StepConfiguration(trimmed);
    }

    /// <summary>
    /// Creates an empty configuration.
    /// </summary>
    public static StepConfiguration Empty => new("{}");

    /// <summary>
    /// Gets the configuration as a dictionary.
    /// </summary>
    public Dictionary<string, object> ToDictionary()
    {
        if (string.IsNullOrEmpty(Value) || Value == "{}")
            return new Dictionary<string, object>();

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object>>(Value)
                   ?? new Dictionary<string, object>();
        }
        catch
        {
            return new Dictionary<string, object>();
        }
    }

    /// <summary>
    /// Gets a configuration value by key.
    /// </summary>
    public T? GetValue<T>(string key, T? defaultValue = default)
    {
        try
        {
            using var doc = JsonDocument.Parse(Value);
            if (doc.RootElement.TryGetProperty(key, out var property))
            {
                return JsonSerializer.Deserialize<T>(property.GetRawText());
            }
        }
        catch
        {
            // Return default on any error
        }

        return defaultValue;
    }

    private static Result ValidateForStepType(JsonDocument doc, StepType stepType)
    {
        var root = doc.RootElement;

        // Validate specific step types
        if (stepType == StepType.Motif)
        {
            if (!root.TryGetProperty("pattern", out var pattern) ||
                pattern.ValueKind != JsonValueKind.String ||
                string.IsNullOrWhiteSpace(pattern.GetString()))
            {
                return Result.Failure(PipelineErrors.MotifPatternRequired);
            }
        }
        else if (stepType == StepType.Restriction)
        {
            if (!root.TryGetProperty("enzymes", out var enzymes) ||
                enzymes.ValueKind != JsonValueKind.Array ||
                enzymes.GetArrayLength() == 0)
            {
                return Result.Failure(PipelineErrors.RestrictionEnzymesRequired);
            }
        }
        else if (stepType == StepType.Trimming)
        {
            if (root.TryGetProperty("cutoff", out var cutoff) && cutoff.ValueKind == JsonValueKind.Number)
            {
                var value = cutoff.GetDouble();
                if (value < 0.01 || value > 0.5)
                {
                    return Result.Failure(PipelineErrors.TrimmingCutoffOutOfRange);
                }
            }
        }

        return Result.Success();
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
