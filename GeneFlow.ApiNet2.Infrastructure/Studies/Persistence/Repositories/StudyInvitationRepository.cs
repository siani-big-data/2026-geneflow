using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Studies.Entities;
using GeneFlow.ApiNet2.Domain.Studies.Enumerations;
using GeneFlow.ApiNet2.Infrastructure.Studies.Persistence.Context;
using GeneFlow.ApiNet2.SharedKernel.Domain.Pagination;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GeneFlow.ApiNet2.Infrastructure.Studies.Persistence.Repositories;

/// <summary>
/// Repository implementation for StudyInvitation aggregate.
/// </summary>
public sealed class StudyInvitationRepository : IStudyInvitationRepository
{
    private readonly StudyContext _context;
    private readonly ILogger<StudyInvitationRepository> _logger;

    /// <summary>
    /// Initializes a new instance of the StudyInvitationRepository.
    /// </summary>
    public StudyInvitationRepository(StudyContext context, ILogger<StudyInvitationRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<StudyInvitation?> GetByIdAsync(StudyInvitationId id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting invitation by ID: {InvitationId}", id);
        return await _context.StudyInvitations
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<StudyInvitation?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting invitation by token");
        return await _context.StudyInvitations
            .FirstOrDefaultAsync(i => i.Token == token, cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(StudyInvitation invitation, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Adding new invitation: {InvitationId}", invitation.Id);
        await _context.StudyInvitations.AddAsync(invitation, cancellationToken);
    }

    /// <inheritdoc />
    public void Update(StudyInvitation invitation)
    {
        _logger.LogDebug("Updating invitation: {InvitationId}", invitation.Id);
        _context.StudyInvitations.Update(invitation);
    }

    /// <inheritdoc />
    public void Delete(StudyInvitation invitation)
    {
        _logger.LogDebug("Deleting invitation: {InvitationId}", invitation.Id);
        _context.StudyInvitations.Remove(invitation);
    }

    /// <inheritdoc />
    public async Task<PagedList<StudyInvitation>> GetByStudyAsync(
        StudyId studyId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting invitations for study: {StudyId}", studyId);

        var query = _context.StudyInvitations
            .Where(i => i.StudyId == studyId)
            .OrderByDescending(i => i.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return PagedList<StudyInvitation>.Create(items, pageNumber, pageSize, totalCount);
    }

    /// <inheritdoc />
    public async Task<PagedList<StudyInvitation>> GetPendingByEmailAsync(
        string email,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting pending invitations for email: {Email}", email);

        var normalizedEmail = email.ToLowerInvariant();
        var query = _context.StudyInvitations
            .Where(i => i.Email.ToLower() == normalizedEmail && i.Status == InvitationStatus.Pending)
            .OrderByDescending(i => i.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return PagedList<StudyInvitation>.Create(items, pageNumber, pageSize, totalCount);
    }

    /// <inheritdoc />
    public async Task<bool> HasPendingInvitationAsync(
        StudyId studyId,
        string email,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.ToLowerInvariant();
        return await _context.StudyInvitations
            .AnyAsync(i => i.StudyId == studyId &&
                          i.Email.ToLower() == normalizedEmail &&
                          i.Status == InvitationStatus.Pending,
                cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<StudyInvitation>> GetExpiredPendingAsync(
        DateTime cutoffTime,
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting expired pending invitations before: {CutoffTime}", cutoffTime);

        return await _context.StudyInvitations
            .Where(i => i.Status == InvitationStatus.Pending && i.ExpiresAt < cutoffTime)
            .OrderBy(i => i.ExpiresAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }
}
