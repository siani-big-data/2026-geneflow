using GeneFlow.ApiNet2.Domain.Identity;
using GeneFlow.ApiNet2.Domain.Identity.Enumerations;
using GeneFlow.ApiNet2.Domain.Identity.ValueObjects;
using GeneFlow.ApiNet2.Infrastructure.Identity.Persistence.Context;
using GeneFlow.ApiNet2.Infrastructure.Identity.Persistence.Repositories;
using GeneFlow.ApiNet2.Tests.Common.Builders;
using GeneFlow.ApiNet2.Tests.Common.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace GeneFlow.ApiNet2.Tests.Infrastructure.Repositories;

/// <summary>
/// Integration tests for UserRepository with real PostgreSQL database.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public class UserRepositoryTests : RepositoryTestBase
{
    private UserContext _context = null!;
    private UserRepository _repository = null!;

    public UserRepositoryTests(
        PostgreSqlContainerFixture postgresFixture,
        RedisContainerFixture redisFixture)
        : base(postgresFixture, redisFixture)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        var options = CreateDbContextOptions<UserContext>();
        _context = new UserContext(options);

        // Ensure database is created
        await _context.Database.EnsureCreatedAsync();

        _repository = new UserRepository(_context, CreateLogger<UserRepository>());
    }

    public override async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await base.DisposeAsync();
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ExistingUser_ShouldReturnUser()
    {
        // Arrange
        var user = UserBuilder.Default()
            .WithId(1)
            .WithEmail("existing@example.com")
            .WithUsername("existinguser")
            .Build();

        await _repository.AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(user.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
        result.Email.Value.Should().Be("existing@example.com");
        result.Username.Value.Should().Be("existinguser");
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingUser_ShouldReturnNull()
    {
        // Arrange
        var nonExistingId = new UserId(999999);

        // Act
        var result = await _repository.GetByIdAsync(nonExistingId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetByEmailAsync Tests

    [Fact]
    public async Task GetByEmailAsync_ExistingEmail_ShouldReturnUser()
    {
        // Arrange
        var user = UserBuilder.Default()
            .WithId(2)
            .WithEmail("findme@example.com")
            .WithUsername("findmeuser")
            .Build();

        await _repository.AddAsync(user);
        await _context.SaveChangesAsync();

        var email = Email.Create("findme@example.com").Value;

        // Act
        var result = await _repository.GetByEmailAsync(email);

        // Assert
        result.Should().NotBeNull();
        result!.Email.Value.Should().Be("findme@example.com");
    }

    [Fact]
    public async Task GetByEmailAsync_NonExistingEmail_ShouldReturnNull()
    {
        // Arrange
        var email = Email.Create("nonexistent@example.com").Value;

        // Act
        var result = await _repository.GetByEmailAsync(email);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByEmailAsync_CaseInsensitive_ShouldReturnUser()
    {
        // Arrange
        var user = UserBuilder.Default()
            .WithId(3)
            .WithEmail("CaseTest@Example.COM")
            .WithUsername("casetestuser")
            .Build();

        await _repository.AddAsync(user);
        await _context.SaveChangesAsync();

        var email = Email.Create("casetest@example.com").Value;

        // Act
        var result = await _repository.GetByEmailAsync(email);

        // Assert
        result.Should().NotBeNull();
    }

    #endregion

    #region AddAsync Tests

    [Fact]
    public async Task AddAsync_ValidUser_ShouldPersistUser()
    {
        // Arrange
        var user = UserBuilder.Default()
            .WithId(4)
            .WithEmail("newuser@example.com")
            .WithUsername("newuser")
            .Build();

        // Act
        await _repository.AddAsync(user);
        await _context.SaveChangesAsync();

        // Assert - Verify by querying directly
        var persisted = await _context.Users.FirstOrDefaultAsync(u => u.Id == user.Id);
        persisted.Should().NotBeNull();
        persisted!.Email.Value.Should().Be("newuser@example.com");
        persisted.Username.Value.Should().Be("newuser");
        persisted.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task AddAsync_UserWithRoles_ShouldPersistRoles()
    {
        // Arrange
        var user = UserBuilder.Default()
            .WithId(5)
            .WithEmail("withroles@example.com")
            .WithUsername("withroles")
            .Build();

        user.AddRole(Role.Admin);

        // Act
        await _repository.AddAsync(user);
        await _context.SaveChangesAsync();

        // Assert
        var persisted = await _repository.GetByIdAsync(user.Id);
        persisted.Should().NotBeNull();
        persisted!.HasRole(Role.User).Should().BeTrue();
        persisted.HasRole(Role.Admin).Should().BeTrue();
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_ModifiedUser_ShouldPersistChanges()
    {
        // Arrange
        var user = UserBuilder.Default()
            .WithId(6)
            .WithEmail("toupdate@example.com")
            .WithUsername("toupdate")
            .Build();

        await _repository.AddAsync(user);
        await _context.SaveChangesAsync();

        // Detach to simulate a new context
        _context.Entry(user).State = EntityState.Detached;

        // Load the user again and modify
        var loadedUser = await _repository.GetByIdAsync(user.Id);
        loadedUser!.Deactivate();

        // Act
        _repository.Update(loadedUser);
        await _context.SaveChangesAsync();

        // Assert
        _context.Entry(loadedUser).State = EntityState.Detached;
        var updated = await _repository.GetByIdAsync(user.Id);
        updated.Should().NotBeNull();
        updated!.IsActive.Should().BeFalse();
    }

    #endregion

    #region ExistsWithEmailAsync Tests

    [Fact]
    public async Task ExistsWithEmailAsync_ExistingEmail_ShouldReturnTrue()
    {
        // Arrange
        var user = UserBuilder.Default()
            .WithId(7)
            .WithEmail("exists@example.com")
            .WithUsername("existsuser")
            .Build();

        await _repository.AddAsync(user);
        await _context.SaveChangesAsync();

        var email = Email.Create("exists@example.com").Value;

        // Act
        var result = await _repository.ExistsWithEmailAsync(email);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsWithEmailAsync_NonExistingEmail_ShouldReturnFalse()
    {
        // Arrange
        var email = Email.Create("doesnotexist@example.com").Value;

        // Act
        var result = await _repository.ExistsWithEmailAsync(email);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region ExistsWithUsernameAsync Tests

    [Fact]
    public async Task ExistsWithUsernameAsync_ExistingUsername_ShouldReturnTrue()
    {
        // Arrange
        var user = UserBuilder.Default()
            .WithId(8)
            .WithEmail("usernamecheck@example.com")
            .WithUsername("checkusername")
            .Build();

        await _repository.AddAsync(user);
        await _context.SaveChangesAsync();

        var username = Username.Create("checkusername").Value;

        // Act
        var result = await _repository.ExistsWithUsernameAsync(username);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsWithUsernameAsync_NonExistingUsername_ShouldReturnFalse()
    {
        // Arrange
        var username = Username.Create("nonexistentuser").Value;

        // Act
        var result = await _repository.ExistsWithUsernameAsync(username);

        // Assert
        result.Should().BeFalse();
    }

    #endregion
}
