using GeneFlow.ApiNet2.SharedKernel.Application.CQRS;
using GeneFlow.ApiNet2.SharedKernel.Domain.Results;

namespace GeneFlow.ApiNet2.Application.Analysis.Commands.RequestAlignment;

/// <summary>
/// Command to request a sequence alignment job.
/// </summary>
/// <param name="TraceIds">The trace IDs to align (minimum 2).</param>
/// <param name="Type">Alignment type: pairwise or multiple.</param>
/// <param name="BuildConsensus">Whether to build consensus sequence.</param>
/// <param name="ConsensusMethod">Consensus method: majority or threshold.</param>
/// <param name="MatchScore">Score for matching bases.</param>
/// <param name="MismatchPenalty">Penalty for mismatches.</param>
/// <param name="GapPenalty">Penalty for gaps.</param>
public sealed record RequestAlignmentCommand(
    List<string> TraceIds,
    string Type,
    bool BuildConsensus,
    string ConsensusMethod,
    int MatchScore,
    int MismatchPenalty,
    int GapPenalty) : ICommand<Result<string>>;

/// <summary>
/// Available alignment types.
/// </summary>
public static class AlignmentTypes
{
    public const string Pairwise = "pairwise";
    public const string Multiple = "multiple";

    public static readonly HashSet<string> All = new(StringComparer.OrdinalIgnoreCase)
    {
        Pairwise, Multiple
    };

    public static bool IsValid(string type) => All.Contains(type);
}
