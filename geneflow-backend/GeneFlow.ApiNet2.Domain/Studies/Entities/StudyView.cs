using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.SharedKernel.Domain.DDD;

namespace GeneFlow.ApiNet2.Domain.Studies.Entities;

/// <summary>
/// Represents a view/visit to a study for analytics.
/// </summary>
public sealed class StudyView : Entity<long>
{
    public StudyId StudyId { get; private set; } = null!;
    public UserId? UserId { get; private set; }
    public string? IpHash { get; private set; }
    public string? UserAgent { get; private set; }
    public DateTime ViewedAt { get; private set; }

    private StudyView() : base() { }

    private StudyView(StudyId studyId, UserId? userId, string? ipHash, string? userAgent) : base(0)
    {
        StudyId = studyId;
        UserId = userId;
        IpHash = ipHash;
        UserAgent = userAgent?.Length > 500 ? userAgent[..500] : userAgent;
        ViewedAt = DateTime.UtcNow;
    }

    public static StudyView CreateForUser(StudyId studyId, UserId userId, string? userAgent)
    {
        return new StudyView(studyId, userId, null, userAgent);
    }

    public static StudyView CreateForAnonymous(StudyId studyId, string ipHash, string? userAgent)
    {
        return new StudyView(studyId, null, ipHash, userAgent);
    }
}
