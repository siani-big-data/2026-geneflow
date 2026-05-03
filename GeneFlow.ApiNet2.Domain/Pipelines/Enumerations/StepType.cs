using GeneFlow.ApiNet2.SharedKernel.Domain.Types;

namespace GeneFlow.ApiNet2.Domain.Pipelines.Enumerations;

/// <summary>
/// Types of analysis steps available in pipelines.
/// Maps directly to analyzers in the Analysis module.
/// </summary>
public sealed class StepType : Enumeration<StepType>
{
    public static readonly StepType Quality = new(1, nameof(Quality), "Quality Analysis", "quality", false);
    public static readonly StepType Trimming = new(2, nameof(Trimming), "Sequence Trimming", "trimming", true);
    public static readonly StepType Heterozygote = new(3, nameof(Heterozygote), "Heterozygote Detection", "heterozygote", true);
    public static readonly StepType Motif = new(4, nameof(Motif), "Motif Search", "motif", true);
    public static readonly StepType Translation = new(5, nameof(Translation), "Sequence Translation", "translation", true);
    public static readonly StepType ORF = new(6, nameof(ORF), "Open Reading Frame Detection", "orf", true);
    public static readonly StepType Restriction = new(7, nameof(Restriction), "Restriction Site Analysis", "restriction", true);

    /// <summary>
    /// Human-readable display name.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// The analysis key used by the Python Analysis module.
    /// </summary>
    public string AnalysisKey { get; }

    /// <summary>
    /// Whether this step type requires configuration parameters.
    /// </summary>
    public bool RequiresConfiguration { get; }

    private StepType(int id, string name, string displayName, string analysisKey, bool requiresConfiguration)
        : base(id, name)
    {
        DisplayName = displayName;
        AnalysisKey = analysisKey;
        RequiresConfiguration = requiresConfiguration;
    }

    /// <summary>
    /// Gets the JSON schema for this step type's configuration.
    /// </summary>
    public string GetConfigurationSchema()
    {
        return this.Name switch
        {
            nameof(Quality) => "{}",
            nameof(Trimming) => """
                {
                    "type": "object",
                    "properties": {
                        "algorithm": { "type": "string", "enum": ["mott", "lucy"], "default": "mott" },
                        "cutoff": { "type": "number", "minimum": 0.01, "maximum": 0.5, "default": 0.05 }
                    }
                }
                """,
            nameof(Heterozygote) => """
                {
                    "type": "object",
                    "properties": {
                        "min_ratio": { "type": "number", "minimum": 0.1, "maximum": 0.5, "default": 0.3 },
                        "max_ratio": { "type": "number", "minimum": 0.5, "maximum": 0.9, "default": 0.7 }
                    }
                }
                """,
            nameof(Motif) => """
                {
                    "type": "object",
                    "required": ["pattern"],
                    "properties": {
                        "pattern": { "type": "string", "minLength": 1 },
                        "search_complement": { "type": "boolean", "default": false }
                    }
                }
                """,
            nameof(Translation) => """
                {
                    "type": "object",
                    "properties": {
                        "frame": { "type": "integer", "enum": [1, 2, 3, -1, -2, -3], "default": 1 }
                    }
                }
                """,
            nameof(ORF) => """
                {
                    "type": "object",
                    "properties": {
                        "min_length": { "type": "integer", "minimum": 30, "default": 100 },
                        "frames": { "type": "array", "items": { "type": "integer", "enum": [1, 2, 3, -1, -2, -3] }, "default": [1, 2, 3] }
                    }
                }
                """,
            nameof(Restriction) => """
                {
                    "type": "object",
                    "required": ["enzymes"],
                    "properties": {
                        "enzymes": { "type": "array", "items": { "type": "string" }, "minItems": 1 }
                    }
                }
                """,
            _ => "{}"
        };
    }
}
