using Testcontainers.Redis;

namespace GeneFlow.ApiNet2.Tests.Common.Fixtures;

/// <summary>
/// Fixture for managing a Redis testcontainer for integration tests.
/// </summary>
public class RedisContainerFixture : IAsyncLifetime
{
    private readonly RedisContainer _container;

    public RedisContainerFixture()
    {
        _container = new RedisBuilder()
            .WithImage("redis:7-alpine")
            .WithCleanUp(true)
            .Build();
    }

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}
