namespace GeneFlow.ApiNet2.API.Contracts.Studies.Requests;

/// <summary>
/// Request model for updating study settings.
/// </summary>
public sealed record UpdateStudySettingsRequest
{
    /// <summary>Whether to allow public comments on the study.</summary>
    public bool AllowPublicComments { get; init; } = true;

    /// <summary>Whether to allow data downloads.</summary>
    public bool AllowDataDownload { get; init; } = false;

    /// <summary>Whether to require approval to join the study.</summary>
    public bool RequireApprovalToJoin { get; init; } = true;
}
