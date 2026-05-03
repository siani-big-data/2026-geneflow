namespace GeneFlow.ApiNet2.API.Contracts.Analysis.Requests;

/// <summary>
/// Request to perform sequence alignment.
/// </summary>
/// <param name="Type">Alignment type: pairwise or multiple. Default: pairwise.</param>
/// <param name="TraceIds">List of trace IDs to align. Required, minimum 2.</param>
/// <param name="BuildConsensus">Build consensus sequence. Default: true.</param>
/// <param name="ConsensusMethod">Consensus method: majority or threshold. Default: majority.</param>
/// <param name="MatchScore">Score for matching bases. Default: 1.</param>
/// <param name="MismatchPenalty">Penalty for mismatches. Default: -1.</param>
/// <param name="GapPenalty">Penalty for gaps. Default: -2.</param>
public sealed record AlignmentRequest(
    string Type = "pairwise",
    List<string>? TraceIds = null,
    bool BuildConsensus = true,
    string ConsensusMethod = "majority",
    int MatchScore = 1,
    int MismatchPenalty = -1,
    int GapPenalty = -2);
