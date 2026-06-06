using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Identity.Entities;
using GeneFlow.ApiNet2.Domain.Identity.Enumerations;
using GeneFlow.ApiNet2.Domain.Identity.ValueObjects;
using GeneFlow.ApiNet2.Infrastructure.Identity.Persistence.Context;
using GeneFlow.ApiNet2.SharedKernel.Domain.Pagination;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GeneFlow.ApiNet2.Infrastructure.Identity.Persistence.Repositories;

/// <summary>
/// Repository implementation for User aggregate.
/// </summary>
public sealed class UserRepository : IUserRepository
{
    private readonly UserContext _context;
    private readonly ILogger<UserRepository> _logger;

    /// <summary>
    /// Initializes a new instance of the UserRepository.
    /// </summary>
    public UserRepository(UserContext context, ILogger<UserRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(User entity, CancellationToken cancellationToken = default)
    {
        await _context.Users.AddAsync(entity, cancellationToken);
    }

    /// <inheritdoc />
    public void Update(User entity)
    {
        _context.Users.Update(entity);
    }

    /// <inheritdoc />
    public void Remove(User entity)
    {
        _context.Users.Remove(entity);
    }

    /// <inheritdoc />
    public async Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GetByEmailAsync: Starting for email {Email}", email.Value);
        var userId = await GetUserIdByEmailAsync(email.Value, cancellationToken);
        _logger.LogInformation("GetByEmailAsync: Got userId {UserId}", userId ?? "null");
        if (userId == null) return null;
        _logger.LogInformation("GetByEmailAsync: Fetching user from EF Core");
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == UserId.Parse(userId), cancellationToken);
        _logger.LogInformation("GetByEmailAsync: Done, user found: {Found}", user != null);
        return user;
    }

    /// <inheritdoc />
    public async Task<User?> GetByEmailStringAsync(string email, CancellationToken cancellationToken = default)
    {
        var userId = await GetUserIdByEmailAsync(email.Trim(), cancellationToken);
        if (userId == null) return null;
        return await _context.Users.FirstOrDefaultAsync(u => u.Id == UserId.Parse(userId), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<User?> GetByUsernameAsync(Username username, CancellationToken cancellationToken = default)
    {
        var userId = await GetUserIdByUsernameAsync(username.Value, cancellationToken);
        if (userId == null) return null;
        return await _context.Users.FirstOrDefaultAsync(u => u.Id == UserId.Parse(userId), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<User?> GetByEmailOrUsernameAsync(string identifier, CancellationToken cancellationToken = default)
    {
        var normalizedIdentifier = identifier.Trim();
        var userId = await GetUserIdByEmailOrUsernameAsync(normalizedIdentifier, cancellationToken);
        if (userId == null) return null;
        return await _context.Users.FirstOrDefaultAsync(u => u.Id == UserId.Parse(userId), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsWithEmailAsync(Email email, CancellationToken cancellationToken = default)
    {
        var userId = await GetUserIdByEmailAsync(email.Value, cancellationToken);
        return userId != null;
    }

    /// <inheritdoc />
    public async Task<bool> ExistsWithUsernameAsync(Username username, CancellationToken cancellationToken = default)
    {
        var userId = await GetUserIdByUsernameAsync(username.Value, cancellationToken);
        return userId != null;
    }

    private async Task<string?> GetUserIdByEmailAsync(string email, CancellationToken cancellationToken)
    {
        _logger.LogInformation("GetUserIdByEmailAsync: Starting query for {Email}", email);
        var connection = _context.Database.GetDbConnection();
        _logger.LogInformation("GetUserIdByEmailAsync: Connection state is {State}", connection.State);
        if (connection.State != System.Data.ConnectionState.Open)
        {
            _logger.LogInformation("GetUserIdByEmailAsync: Opening connection");
            await connection.OpenAsync(cancellationToken);
        }

        _logger.LogInformation("GetUserIdByEmailAsync: Creating command");
        await using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT id FROM identity.users
            WHERE LOWER(email) = LOWER(@email) AND is_deleted = false
            LIMIT 1";

        var param = command.CreateParameter();
        param.ParameterName = "@email";
        param.Value = email;
        command.Parameters.Add(param);

        _logger.LogInformation("GetUserIdByEmailAsync: Executing query");
        var result = await command.ExecuteScalarAsync(cancellationToken);
        _logger.LogInformation("GetUserIdByEmailAsync: Query complete, result is {Result}", result ?? "null");
        return result == null || result == DBNull.Value ? null : result.ToString();
    }

    private async Task<string?> GetUserIdByUsernameAsync(string username, CancellationToken cancellationToken)
    {
        var connection = _context.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT id FROM identity.users
            WHERE LOWER(username) = LOWER(@username) AND is_deleted = false
            LIMIT 1";

        var param = command.CreateParameter();
        param.ParameterName = "@username";
        param.Value = username;
        command.Parameters.Add(param);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result == null || result == DBNull.Value ? null : result.ToString();
    }

    private async Task<string?> GetUserIdByEmailOrUsernameAsync(string identifier, CancellationToken cancellationToken)
    {
        var connection = _context.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT id FROM identity.users
            WHERE (LOWER(email) = LOWER(@identifier) OR LOWER(username) = LOWER(@identifier))
            AND is_deleted = false
            LIMIT 1";

        var param = command.CreateParameter();
        param.ParameterName = "@identifier";
        param.Value = identifier;
        command.Parameters.Add(param);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result == null || result == DBNull.Value ? null : result.ToString();
    }

    /// <inheritdoc />
    public async Task<User?> GetByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        // Query using raw ADO.NET since RefreshTokens is an owned collection mapped via backing field
        // EF Core's SqlQuery<T> wraps primitive types and generates incorrect SQL
        var connection = _context.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT ""UserId"" FROM identity.refresh_tokens
            WHERE token = @token
            LIMIT 1";

        var param = command.CreateParameter();
        param.ParameterName = "@token";
        param.Value = refreshToken;
        command.Parameters.Add(param);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        if (result == null || result == DBNull.Value)
            return null;

        var userIdString = result.ToString();
        if (string.IsNullOrEmpty(userIdString))
            return null;

        if (!UserId.TryParse(userIdString, out var userId) || userId is null)
            return null;

        return await GetByIdAsync(userId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<User?> GetByEmailVerificationTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.EmailVerification.Token == token, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<User?> GetByPasswordResetTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.PasswordReset.Token == token, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<User>> GetByIdsAsync(IEnumerable<UserId> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();
        return await _context.Users
            .Where(u => idList.Contains(u.Id))
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<User?> GetByExternalLoginAsync(
        ExternalProvider provider,
        string providerKey,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GetByExternalLoginAsync: Starting for provider {Provider}", provider.Name);

        // Query through the external_logins table directly using ADO.NET
        // since ExternalLogins is an owned collection with a backing field
        var connection = _context.Database.GetDbConnection();

        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT ""UserId""
            FROM identity.external_logins
            WHERE provider = @provider AND provider_key = @providerKey
            LIMIT 1";

        var providerParam = command.CreateParameter();
        providerParam.ParameterName = "@provider";
        providerParam.Value = provider.Name;
        command.Parameters.Add(providerParam);

        var keyParam = command.CreateParameter();
        keyParam.ParameterName = "@providerKey";
        keyParam.Value = providerKey;
        command.Parameters.Add(keyParam);

        var result = await command.ExecuteScalarAsync(cancellationToken);

        if (result == null || result == DBNull.Value)
            return null;

        var userId = result.ToString()!;
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Id == UserId.Parse(userId), cancellationToken);
    }

    // ============================================================
    // Follow graph
    // ============================================================

    /// <inheritdoc />
    public async Task<bool> IsFollowingAsync(UserId followerId, UserId followeeId, CancellationToken cancellationToken = default)
    {
        return await _context.UserFollows
            .AsNoTracking()
            .AnyAsync(f => f.FollowerId == followerId && f.FolloweeId == followeeId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddFollowAsync(UserFollow follow, CancellationToken cancellationToken = default)
    {
        await _context.UserFollows.AddAsync(follow, cancellationToken);
    }

    /// <inheritdoc />
    public async Task RemoveFollowAsync(UserId followerId, UserId followeeId, CancellationToken cancellationToken = default)
    {
        var edge = await _context.UserFollows
            .FirstOrDefaultAsync(f => f.FollowerId == followerId && f.FolloweeId == followeeId, cancellationToken);

        if (edge is not null)
            _context.UserFollows.Remove(edge);
    }

    /// <inheritdoc />
    public async Task<int> CountFollowersAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserFollows
            .AsNoTracking()
            .CountAsync(f => f.FolloweeId == userId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> CountFollowingAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserFollows
            .AsNoTracking()
            .CountAsync(f => f.FollowerId == userId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PagedList<User>> GetFollowersAsync(
        UserId userId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var followerIds = _context.UserFollows
            .AsNoTracking()
            .Where(f => f.FolloweeId == userId)
            .OrderByDescending(f => f.FollowedAt)
            .Select(f => f.FollowerId);

        var totalCount = await followerIds.CountAsync(cancellationToken);

        var pagedIds = await followerIds
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var users = await _context.Users
            .AsNoTracking()
            .Where(u => pagedIds.Contains(u.Id))
            .ToListAsync(cancellationToken);

        // Preserve the page order (sorted by FollowedAt desc).
        var ordered = pagedIds
            .Select(id => users.FirstOrDefault(u => u.Id == id))
            .Where(u => u is not null)
            .Select(u => u!)
            .ToList();

        return PagedList<User>.Create(ordered, pageNumber, pageSize, totalCount);
    }

    /// <inheritdoc />
    public async Task<PagedList<User>> GetFollowingAsync(
        UserId userId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var followeeIds = _context.UserFollows
            .AsNoTracking()
            .Where(f => f.FollowerId == userId)
            .OrderByDescending(f => f.FollowedAt)
            .Select(f => f.FolloweeId);

        var totalCount = await followeeIds.CountAsync(cancellationToken);

        var pagedIds = await followeeIds
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var users = await _context.Users
            .AsNoTracking()
            .Where(u => pagedIds.Contains(u.Id))
            .ToListAsync(cancellationToken);

        var ordered = pagedIds
            .Select(id => users.FirstOrDefault(u => u.Id == id))
            .Where(u => u is not null)
            .Select(u => u!)
            .ToList();

        return PagedList<User>.Create(ordered, pageNumber, pageSize, totalCount);
    }

    public async Task<IReadOnlyList<UserId>> GetFollowingIdsAsync(
        UserId userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.UserFollows
            .AsNoTracking()
            .Where(f => f.FollowerId == userId)
            .Select(f => f.FolloweeId)
            .ToListAsync(cancellationToken);
    }
}
