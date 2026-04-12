using GeneFlow.ApiNet2.SharedKernel.Domain.DDD;

namespace GeneFlow.ApiNet2.Domain.Studies.ValueObjects;

/// <summary>
/// Represents study configuration settings.
/// </summary>
public sealed class StudySettings : ValueObject
{
    public bool AllowPublicComments { get; }
    public bool AllowDataDownload { get; }
    public bool RequireApprovalToJoin { get; }

    private StudySettings(bool allowPublicComments, bool allowDataDownload, bool requireApprovalToJoin)
    {
        AllowPublicComments = allowPublicComments;
        AllowDataDownload = allowDataDownload;
        RequireApprovalToJoin = requireApprovalToJoin;
    }

    public static StudySettings Create(
        bool allowPublicComments = true,
        bool allowDataDownload = false,
        bool requireApprovalToJoin = true)
    {
        return new StudySettings(allowPublicComments, allowDataDownload, requireApprovalToJoin);
    }

    public static StudySettings Default => new(true, false, true);

    public StudySettings WithAllowPublicComments(bool value) => new(value, AllowDataDownload, RequireApprovalToJoin);
    public StudySettings WithAllowDataDownload(bool value) => new(AllowPublicComments, value, RequireApprovalToJoin);
    public StudySettings WithRequireApprovalToJoin(bool value) => new(AllowPublicComments, AllowDataDownload, value);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return AllowPublicComments;
        yield return AllowDataDownload;
        yield return RequireApprovalToJoin;
    }
}
