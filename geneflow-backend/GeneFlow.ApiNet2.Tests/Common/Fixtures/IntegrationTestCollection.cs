namespace GeneFlow.ApiNet2.Tests.Common.Fixtures;

/// <summary>
/// xUnit collection definition for integration tests that share database containers.
/// </summary>
[CollectionDefinition(Name)]
public class IntegrationTestCollection : ICollectionFixture<PostgreSqlContainerFixture>,
                                          ICollectionFixture<RedisContainerFixture>
{
    public const string Name = "Integration";
}

/// <summary>
/// xUnit collection definition for E2E tests.
/// </summary>
[CollectionDefinition(Name)]
public class E2ETestCollection : ICollectionFixture<PostgreSqlContainerFixture>,
                                  ICollectionFixture<RedisContainerFixture>
{
    public const string Name = "E2E";
}
