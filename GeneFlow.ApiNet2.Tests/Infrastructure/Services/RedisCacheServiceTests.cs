using System.Text.Json;
using GeneFlow.ApiNet2.Infrastructure.Redis;
using StackExchange.Redis;

namespace GeneFlow.ApiNet2.Tests.Infrastructure.Services;

/// <summary>
/// Unit tests for the RedisCacheService.
/// Uses mocks to test cache operations without a real Redis instance.
/// </summary>
public class RedisCacheServiceTests
{
    private readonly IConnectionMultiplexer _redisMock;
    private readonly IDatabase _databaseMock;
    private readonly IServer _serverMock;
    private readonly RedisCacheService _service;

    public RedisCacheServiceTests()
    {
        _redisMock = Substitute.For<IConnectionMultiplexer>();
        _databaseMock = Substitute.For<IDatabase>();
        _serverMock = Substitute.For<IServer>();

        _redisMock.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(_databaseMock);
        _redisMock.GetEndPoints(Arg.Any<bool>()).Returns(new[] { new System.Net.DnsEndPoint("localhost", 6379) });
        _redisMock.GetServer(Arg.Any<System.Net.EndPoint>(), Arg.Any<object>()).Returns(_serverMock);

        _service = new RedisCacheService(_redisMock);
    }

    #region SetAsync

    [Fact]
    public async Task SetAsync_ShouldStoreSerializedValue()
    {
        // Arrange
        var key = "test:key";
        var value = new TestData { Id = 1, Name = "Test" };

        _databaseMock.StringSetAsync(
            Arg.Any<RedisKey>(),
            Arg.Any<RedisValue>(),
            Arg.Any<TimeSpan?>(),
            Arg.Any<bool>(),
            Arg.Any<When>(),
            Arg.Any<CommandFlags>())
            .Returns(true);

        // Act
        await _service.SetAsync(key, value);

        // Assert
        await _databaseMock.Received(1).StringSetAsync(
            Arg.Is<RedisKey>(k => k == key),
            Arg.Any<RedisValue>(),
            Arg.Any<TimeSpan?>(),
            Arg.Any<bool>(),
            Arg.Any<When>(),
            Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task SetAsync_WithExpiration_ShouldPassExpiration()
    {
        // Arrange
        var key = "test:key";
        var value = "test value";
        var expiration = TimeSpan.FromMinutes(30);

        _databaseMock.StringSetAsync(
            Arg.Any<RedisKey>(),
            Arg.Any<RedisValue>(),
            Arg.Any<TimeSpan?>(),
            Arg.Any<bool>(),
            Arg.Any<When>(),
            Arg.Any<CommandFlags>())
            .Returns(true);

        // Act
        await _service.SetAsync(key, value, expiration);

        // Assert
        await _databaseMock.Received(1).StringSetAsync(
            Arg.Any<RedisKey>(),
            Arg.Any<RedisValue>(),
            expiration,
            Arg.Any<bool>(),
            Arg.Any<When>(),
            Arg.Any<CommandFlags>());
    }

    #endregion

    #region GetAsync

    [Fact]
    public async Task GetAsync_WhenKeyExists_ShouldReturnDeserializedValue()
    {
        // Arrange
        var key = "test:key";
        var expectedData = new TestData { Id = 1, Name = "Test" };
        var serialized = JsonSerializer.Serialize(expectedData, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        _databaseMock.StringGetAsync(Arg.Is<RedisKey>(k => k == key), Arg.Any<CommandFlags>())
            .Returns(new RedisValue(serialized));

        // Act
        var result = await _service.GetAsync<TestData>(key);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Name.Should().Be("Test");
    }

    [Fact]
    public async Task GetAsync_WhenKeyNotExists_ShouldReturnDefault()
    {
        // Arrange
        var key = "test:nonexistent";

        _databaseMock.StringGetAsync(Arg.Is<RedisKey>(k => k == key), Arg.Any<CommandFlags>())
            .Returns(RedisValue.Null);

        // Act
        var result = await _service.GetAsync<TestData>(key);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region RemoveAsync

    [Fact]
    public async Task RemoveAsync_ShouldDeleteKey()
    {
        // Arrange
        var key = "test:key";

        _databaseMock.KeyDeleteAsync(Arg.Is<RedisKey>(k => k == key), Arg.Any<CommandFlags>())
            .Returns(true);

        // Act
        await _service.RemoveAsync(key);

        // Assert
        await _databaseMock.Received(1).KeyDeleteAsync(
            Arg.Is<RedisKey>(k => k == key),
            Arg.Any<CommandFlags>());
    }

    #endregion

    #region ExistsAsync

    [Fact]
    public async Task ExistsAsync_WhenKeyExists_ShouldReturnTrue()
    {
        // Arrange
        var key = "test:key";

        _databaseMock.KeyExistsAsync(Arg.Is<RedisKey>(k => k == key), Arg.Any<CommandFlags>())
            .Returns(true);

        // Act
        var result = await _service.ExistsAsync(key);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WhenKeyNotExists_ShouldReturnFalse()
    {
        // Arrange
        var key = "test:nonexistent";

        _databaseMock.KeyExistsAsync(Arg.Is<RedisKey>(k => k == key), Arg.Any<CommandFlags>())
            .Returns(false);

        // Act
        var result = await _service.ExistsAsync(key);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region GetOrCreateAsync

    [Fact]
    public async Task GetOrCreateAsync_WhenKeyExists_ShouldReturnCachedValue()
    {
        // Arrange
        var key = "test:key";
        var cachedData = new TestData { Id = 1, Name = "Cached" };
        var serialized = JsonSerializer.Serialize(cachedData, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        var factoryCalled = false;

        _databaseMock.StringGetAsync(Arg.Is<RedisKey>(k => k == key), Arg.Any<CommandFlags>())
            .Returns(new RedisValue(serialized));

        // Act
        var result = await _service.GetOrCreateAsync(
            key,
            _ =>
            {
                factoryCalled = true;
                return Task.FromResult(new TestData { Id = 2, Name = "Factory" });
            });

        // Assert
        result.Id.Should().Be(1);
        result.Name.Should().Be("Cached");
        factoryCalled.Should().BeFalse();
    }

    [Fact]
    public async Task GetOrCreateAsync_WhenKeyNotExists_ShouldCallFactoryAndCache()
    {
        // Arrange
        var key = "test:key";
        var factoryData = new TestData { Id = 2, Name = "Factory" };

        _databaseMock.StringGetAsync(Arg.Is<RedisKey>(k => k == key), Arg.Any<CommandFlags>())
            .Returns(RedisValue.Null);
        _databaseMock.StringSetAsync(
            Arg.Any<RedisKey>(),
            Arg.Any<RedisValue>(),
            Arg.Any<TimeSpan?>(),
            Arg.Any<bool>(),
            Arg.Any<When>(),
            Arg.Any<CommandFlags>())
            .Returns(true);

        // Act
        var result = await _service.GetOrCreateAsync(
            key,
            _ => Task.FromResult(factoryData));

        // Assert
        result.Id.Should().Be(2);
        result.Name.Should().Be("Factory");
        await _databaseMock.Received(1).StringSetAsync(
            Arg.Any<RedisKey>(),
            Arg.Any<RedisValue>(),
            Arg.Any<TimeSpan?>(),
            Arg.Any<bool>(),
            Arg.Any<When>(),
            Arg.Any<CommandFlags>());
    }

    #endregion

    #region Helper Classes

    private sealed class TestData
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    #endregion
}
