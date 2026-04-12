using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies.Entities;
using GeneFlow.ApiNet2.SharedKernel.Domain.Pagination;

namespace GeneFlow.ApiNet2.Domain.Studies;

/// <summary>
/// Repository interface for StudyInvitation aggregate.
/// </summary>
public interface IStudyInvitationRepository
{
    Task<StudyInvitation?> GetByIdAsync(StudyInvitationId id, CancellationToken cancellationToken = default);
    Task<StudyInvitation?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);
    Task AddAsync(StudyInvitation invitation, CancellationToken cancellationToken = default);
    void Update(StudyInvitation invitation);
    void Delete(StudyInvitation invitation);

    // Get invitations for a study
    Task<PagedList<StudyInvitation>> GetByStudyAsync(
        StudyId studyId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    // Get pending invitations for a user (by email)
    Task<PagedList<StudyInvitation>> GetPendingByEmailAsync(
        string email,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    // Check for existing pending invitation
    Task<bool> HasPendingInvitationAsync(
        StudyId studyId,
        string email,
        CancellationToken cancellationToken = default);

    // Get expired invitations that need to be updated
    Task<IReadOnlyList<StudyInvitation>> GetExpiredPendingAsync(
        DateTime cutoffTime,
        int batchSize,
        CancellationToken cancellationToken = default);
}
