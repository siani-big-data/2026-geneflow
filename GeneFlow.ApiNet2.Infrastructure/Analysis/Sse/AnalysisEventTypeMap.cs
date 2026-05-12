namespace GeneFlow.ApiNet2.Infrastructure.Analysis.Sse;

/// <summary>
/// Maps Python analysis worker event types to the stable analysis-key used by
/// the API and the frontend (e.g. <c>TrimmingCompleted</c> → <c>trimming</c>).
/// Centralised here so both the pipeline orchestrator's listener and the SSE
/// broadcaster can reuse the same translation.
/// </summary>
internal static class AnalysisEventTypeMap
{
    public static readonly IReadOnlyDictionary<string, string> CompletedEventToAnalysisKey =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["TrimmingCompleted"] = "trimming",
            ["HeterozygoteDetectionCompleted"] = "heterozygote",
            ["MotifSearchCompleted"] = "motif",
            ["TranslationCompleted"] = "translation",
            ["ORFDetectionCompleted"] = "orf",
            ["RestrictionAnalysisCompleted"] = "restriction"
        };

    public static readonly IReadOnlyDictionary<string, string> FailedEventToAnalysisKey =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["TrimmingFailed"] = "trimming",
            ["HeterozygoteDetectionFailed"] = "heterozygote",
            ["MotifSearchFailed"] = "motif",
            ["TranslationFailed"] = "translation",
            ["ORFDetectionFailed"] = "orf",
            ["RestrictionAnalysisFailed"] = "restriction"
        };

    /// <summary>
    /// Tries to resolve an analysis key from an event type. Returns true and
    /// sets <paramref name="success"/> accordingly when the event matches a
    /// known completed/failed analysis event.
    /// </summary>
    public static bool TryResolve(string eventType, out string analysisKey, out bool success)
    {
        if (CompletedEventToAnalysisKey.TryGetValue(eventType, out var key))
        {
            analysisKey = key;
            success = true;
            return true;
        }

        if (FailedEventToAnalysisKey.TryGetValue(eventType, out key))
        {
            analysisKey = key;
            success = false;
            return true;
        }

        analysisKey = string.Empty;
        success = false;
        return false;
    }
}
