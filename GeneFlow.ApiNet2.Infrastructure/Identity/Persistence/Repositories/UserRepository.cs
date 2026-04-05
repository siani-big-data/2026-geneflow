using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Identity.Enumerations;
using GeneFlow.ApiNet2.Domain.Identity.ValueObjects;
using GeneFlow.ApiNet2.Infrastructure.Identity.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace GeneFlow.ApiNet2.Infrastructure.Identity.Persistence.Repositories;

/// <summary>
/// Repository implementation for User aggregate.
/// </summary>
public sealed class UserRepository : IUserRepository
{
    private readonly UserContext _context;

    /// <summary>
    /// Initializes a new instance of the UserRepository.
    /// </summary>
    public UserRepository(UserContext context)
    {
        _context = context;
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
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<User?> GetByEmailStringAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        return await _context.Users
            .FirstOrDefaultAsync(u => EF.Property<string>(u, "Email") == normalizedEmail, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<User?> GetByUsernameAsync(Username username, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Username == username, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<User?> GetByEmailOrUsernameAsync(string identifier, CancellationToken cancellationToken = default)
    {
        var normalizedIdentifier = identifier.Trim().ToLowerInvariant();
        return await _context.Users
            .FirstOrDefaultAsync(u =>
                EF.Property<string>(u.Email, "Value") == normalizedIdentifier ||
                EF.Property<string>(u.Username, "Value") == normalizedIdentifier,
                cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsWithEmailAsync(Email email, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AnyAsync(u => u.Email == email, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsWithUsernameAsync(Username username, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AnyAsync(u => u.Username == username, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<User?> GetByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.RefreshTokens.Any(t => t.Token == refreshToken), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<User?> GetByEmailVerificationTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.EmailVerificationToken == token, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<User?> GetByPasswordResetTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.PasswordResetToken == token, cancellationToken);
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
        return await _context.Users
            .FirstOrDefaultAsync(u =>
                u.ExternalLogins.Any(e => e.Provider == provider && e.ProviderKey == providerKey),
                cancellationToken);
    }
}
