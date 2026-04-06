using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Profiles;
using GeneFlow.ApiNet2.Infrastructure.Profiles.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GeneFlow.ApiNet2.Infrastructure.Profiles.Persistence.Repositories;

/// <summary>
/// Repository implementation for Profile aggregate.
/// </summary>
public sealed class ProfileRepository : IProfileRepository
{
    private readonly ProfileContext _context;
    private readonly ILogger<ProfileRepository> _logger;

    /// <summary>
    /// Initializes a new instance of the ProfileRepository.
    /// </summary>
    public ProfileRepository(ProfileContext context, ILogger<ProfileRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Profile?> GetByIdAsync(ProfileId id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting profile by ID: {ProfileId}", id);
        return await _context.Profiles
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(Profile entity, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Adding new profile: {ProfileId}", entity.Id);
        await _context.Profiles.AddAsync(entity, cancellationToken);
    }

    /// <inheritdoc />
    public void Update(Profile entity)
    {
        _logger.LogDebug("Updating profile: {ProfileId}", entity.Id);
        _context.Profiles.Update(entity);
    }

    /// <inheritdoc />
    public void Remove(Profile entity)
    {
        _logger.LogDebug("Removing profile: {ProfileId}", entity.Id);
        _context.Profiles.Remove(entity);
    }

    /// <inheritdoc />
    public async Task<Profile?> GetByUserIdAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting profile by user ID: {UserId}", userId);

        // Use raw SQL for better performance with value object conversion
        var userIdStr = userId.ToString();
        var profileId = await GetProfileIdByUserIdAsync(userIdStr, cancellationToken);

        if (profileId is null)
            return null;

        return await _context.Profiles
            .FirstOrDefaultAsync(p => p.Id == ProfileId.Parse(profileId), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsForUserAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Checking if profile exists for user: {UserId}", userId);

        var userIdStr = userId.ToString();
        var profileId = await GetProfileIdByUserIdAsync(userIdStr, cancellationToken);

        return profileId is not null;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Profile>> GetByUserIdsAsync(
        IEnumerable<UserId> userIds,
        CancellationToken cancellationToken = default)
    {
        var userIdList = userIds.ToList();
        _logger.LogDebug("Getting profiles for {Count} user IDs", userIdList.Count);

        if (userIdList.Count == 0)
            return Array.Empty<Profile>();

        // Get profile IDs for the user IDs
        var userIdStrings = userIdList.Select(id => id.ToString()).ToList();
        var profileIds = await GetProfileIdsByUserIdsAsync(userIdStrings, cancellationToken);

        if (profileIds.Count == 0)
            return Array.Empty<Profile>();

        var parsedProfileIds = profileIds.Select(ProfileId.Parse).ToList();

        return await _context.Profiles
            .Where(p => parsedProfileIds.Contains(p.Id))
            .ToListAsync(cancellationToken);
    }

    private async Task<string?> GetProfileIdByUserIdAsync(string userId, CancellationToken cancellationToken)
    {
        var connection = _context.Database.GetDbConnection();

        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT id FROM profiles.profiles
            WHERE user_id = @userId AND is_deleted = false
            LIMIT 1";

        var param = command.CreateParameter();
        param.ParameterName = "@userId";
        param.Value = userId;
        command.Parameters.Add(param);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result == null || result == DBNull.Value ? null : result.ToString();
    }

    private async Task<List<string>> GetProfileIdsByUserIdsAsync(
        List<string> userIds,
        CancellationToken cancellationToken)
    {
        var connection = _context.Database.GetDbConnection();

        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();

        // Build parameterized query for multiple IDs
        var paramNames = new List<string>();
        for (var i = 0; i < userIds.Count; i++)
        {
            var paramName = $"@userId{i}";
            paramNames.Add(paramName);

            var param = command.CreateParameter();
            param.ParameterName = paramName;
            param.Value = userIds[i];
            command.Parameters.Add(param);
        }

        command.CommandText = $@"
            SELECT id FROM profiles.profiles
            WHERE user_id IN ({string.Join(", ", paramNames)}) AND is_deleted = false";

        var profileIds = new List<string>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            profileIds.Add(reader.GetString(0));
        }

        return profileIds;
    }
}
