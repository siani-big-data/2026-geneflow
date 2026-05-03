using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Analysis.Commands.RequestAnalysis;

/// <summary>
/// Command to request an analysis job for a trace.
/// </summary>
/// <param name="TraceId">The trace ID to analyze.</param>
/// <param name="AnalysisType">The type of analysis to perform.</param>
/// <param name="Options">Analysis-specific options.</param>
public sealed record RequestAnalysisCommand(
    string TraceId,
    string AnalysisType,
    Dictionary<string, object> Options) : ICommand<Result>;

/// <summary>
/// Available analysis types.
/// </summary>
public static class AnalysisTypes
{
    public const string Quality = "quality";
    public const string Trimming = "trimming";
    public const string Heterozygote = "heterozygote";
    public const string Motif = "motif";
    public const string Translation = "translation";
    public const string ORF = "orf";
    public const string Restriction = "restriction";

    public static readonly HashSet<string> All = new(StringComparer.OrdinalIgnoreCase)
    {
        Quality, Trimming, Heterozygote, Motif, Translation, ORF, Restriction
    };


    public static bool IsValid(string type) => All.Contains(type);
}
