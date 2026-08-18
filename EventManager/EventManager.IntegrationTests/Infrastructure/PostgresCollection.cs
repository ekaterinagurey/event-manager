namespace EventManager.IntegrationTests.Infrastructure
{

    [CollectionDefinition("Postgres")]
    public class PostgresCollection : ICollectionFixture<PostgresFixture>
    {
    }
}
