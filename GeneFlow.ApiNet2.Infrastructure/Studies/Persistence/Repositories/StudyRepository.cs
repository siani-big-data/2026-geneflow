using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Studies;
using GeneFlow.ApiNet2.Domain.Studies.Entities;
using GeneFlow.ApiNet2.Domain.Studies.Enumerations;
using GeneFlow.ApiNet2.Infrastructure.Studies.Persistence.Context;
using GeneFlow.ApiNet2.SharedKernel.Domain.Pagination;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GeneFlow.ApiNet2.Infrastructure.Studies.Persistence.Repositories;

/// <summary>
/// Repository implementation for Study aggregate.
/// </summary>
public sealed class StudyRepository : IStudyRepository
{
    private readonly StudyContext _context;
    private readonly ILogger<StudyRepository> _logger;

    /// <summary>
    /// Initializes a new instance of the StudyRepository.
    /// </summary>
    public StudyRepository(StudyContext context, ILogger<StudyRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Study?> GetByIdAsync(StudyId id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting study by ID: {StudyId}", id);
        return await _context.Studies
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Study?> GetByIdWithMembersAsync(StudyId id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting study by ID with members: {StudyId}", id);
        return await _context.Studies
            .Include(s => s.Members)
            .Include(s => s.Papers)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(Study study, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Adding new study: {StudyId}", study.Id);
        await _context.Studies.AddAsync(study, cancellationToken);
    }

    /// <inheritdoc />
    public void Update(Study study)
    {
        _logger.LogDebug("Updating study: {StudyId}", study.Id);
        _context.Studies.Update(study);
    }

    /// <inheritdoc />
    public void Delete(Study study)
    {
        _logger.LogDebug("Deleting study: {StudyId}", study.Id);
        _context.Studies.Remove(study);
    }

    /// <inheritdoc />
    public async Task<PagedList<Study>> GetByMemberAsync(
        UserId userId,
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        StudyStatus? status = null,
        ResearchField? researchField = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting studies for user: {UserId}", userId);

        var userIdStr = userId.ToString();

        // Get study IDs where user is a member
        var studyIds = await GetStudyIdsByMemberAsync(userIdStr, cancellationToken);

        if (studyIds.Count == 0)
            return PagedList<Study>.Create([], pageNumber, pageSize, 0);

        var parsedIds = studyIds.Select(StudyId.Parse).ToList();

        var query = _context.Studies
            .Where(s => parsedIds.Contains(s.Id));

        // Filter by status if provided
        if (status is not null)
        {
            query = query.Where(s => s.Status == status);
        }

        // Filter by research field if provided
        if (researchField is not null)
        {
            query = query.Where(s => s.ResearchField == researchField);
        }

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLowerInvariant();
            query = query.Where(s =>
                EF.Functions.ILike(s.Title.Value, $"%{term}%") ||
                (s.Description != null && EF.Functions.ILike(s.Description.Value, $"%{term}%")));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return PagedList<Study>.Create(items, pageNumber, pageSize, totalCount);
    }

    /// <inheritdoc />
    public async Task<PagedList<Study>> GetPublishedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        ResearchField? researchField = null,
        IReadOnlyList<string>? tags = null,
        string? sortBy = null,
        bool sortDescending = true,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting published studies");

        var query = _context.Studies
            .Where(s => s.Status == StudyStatus.Published);

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLowerInvariant();
            query = query.Where(s =>
                EF.Functions.ILike(s.Title.Value, $"%{term}%") ||
                (s.Description != null && EF.Functions.ILike(s.Description.Value, $"%{term}%")));
        }

        // Apply research field filter
        if (researchField is not null)
        {
            query = query.Where(s => s.ResearchField == researchField);
        }

        // Apply tags filter
        if (tags is { Count: > 0 })
        {
            var tagList = tags.Select(t => t.ToLowerInvariant()).ToList();
            query = query.Where(s => s.Tags.Any(t => tagList.Contains(t.ToLower())));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        // Apply sorting
        query = sortBy?.ToLowerInvariant() switch
        {
            "views" => sortDescending
                ? query.OrderByDescending(s => s.Metrics.ViewsCount)
                : query.OrderBy(s => s.Metrics.ViewsCount),
            "stars" => sortDescending
                ? query.OrderByDescending(s => s.Metrics.StarsCount)
                : query.OrderBy(s => s.Metrics.StarsCount),
            "title" => sortDescending
                ? query.OrderByDescending(s => s.Title.Value)
                : query.OrderBy(s => s.Title.Value),
            _ => sortDescending
                ? query.OrderByDescending(s => s.CreatedAt)
                : query.OrderBy(s => s.CreatedAt)
        };

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return PagedList<Study>.Create(items, pageNumber, pageSize, totalCount);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Study>> GetFeaturedAsync(
        int limit,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting featured studies, limit: {Limit}", limit);

        return await _context.Studies
            .Where(s => s.IsFeatured && s.Status == StudyStatus.Published)
            .OrderByDescending(s => s.Metrics.ViewsCount)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> IsStarredByUserAsync(
        StudyId studyId,
        UserId userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.StudyStars
            .AnyAsync(s => s.StudyId == studyId && s.UserId == userId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddStarAsync(
        StudyId studyId,
        UserId userId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Adding star for study: {StudyId}, user: {UserId}", studyId, userId);
        var star = StudyStar.Create(studyId, userId);
        await _context.StudyStars.AddAsync(star, cancellationToken);
    }

    /// <inheritdoc />
    public async Task RemoveStarAsync(
        StudyId studyId,
        UserId userId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Removing star for study: {StudyId}, user: {UserId}", studyId, userId);

        var star = await _context.StudyStars
            .FirstOrDefaultAsync(s => s.StudyId == studyId && s.UserId == userId, cancellationToken);

        if (star is not null)
        {
            _context.StudyStars.Remove(star);
        }
    }

    /// <inheritdoc />
    public async Task AddViewAsync(
        StudyView view,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Adding view for study: {StudyId}", view.StudyId);
        await _context.StudyViews.AddAsync(view, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> HasRecentViewByUserAsync(
        StudyId studyId,
        UserId userId,
        DateTime cutoffTime,
        CancellationToken cancellationToken = default)
    {
        return await _context.StudyViews
            .AnyAsync(v => v.StudyId == studyId && v.UserId == userId && v.ViewedAt > cutoffTime, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> HasRecentViewByIpHashAsync(
        StudyId studyId,
        string ipHash,
        DateTime cutoffTime,
        CancellationToken cancellationToken = default)
    {
        return await _context.StudyViews
            .AnyAsync(v => v.StudyId == studyId && v.IpHash == ipHash && v.ViewedAt > cutoffTime, cancellationToken);
    }

    private async Task<List<string>> GetStudyIdsByMemberAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        var connection = _context.Database.GetDbConnection();

        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT DISTINCT sm.study_id
            FROM studies.study_members sm
            INNER JOIN studies.studies s ON sm.study_id = s.id
            WHERE sm.user_id = @userId AND s.is_deleted = false";

        var param = command.CreateParameter();
        param.ParameterName = "@userId";
        param.Value = userId;
        command.Parameters.Add(param);

        var studyIds = new List<string>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            studyIds.Add(reader.GetString(0));
        }

        return studyIds;
    }
}
