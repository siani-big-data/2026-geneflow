using Testcontainers.PostgreSql;

namespace GeneFlow.ApiNet2.Tests.Common.Fixtures;

/// <summary>
/// Fixture for managing a PostgreSQL testcontainer for integration tests.
/// </summary>
public class PostgreSqlContainerFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container;

    public PostgreSqlContainerFixture()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("geneflow_test")
            .WithUsername("test")
            .WithPassword("test")
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
