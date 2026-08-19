namespace EventManager.IntegrationTests.Infrastructure
{
    /// <summary>
    /// Атрибут [Collection("PostgresCollection")] ОБЯЗАТЕЛЕН для всех тестовых классов,
    /// работающих с этой фикстурой.
    /// </summary>
    [CollectionDefinition("Postgres")]
    public class PostgresCollection : ICollectionFixture<PostgresFixture>
    {
    }
}
