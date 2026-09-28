using Testcontainers.PostgreSql;

namespace EventManager.IntegrationTests.Infrastructure
{
    public class PostgresFixture : IAsyncLifetime
    {
        private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
         .WithImage("postgres:16-alpine")
         .WithDatabase("eventapi_test")
         .WithUsername("postgres")
         .WithPassword("postgres")
         .Build();

        public string ConnectionString => _postgres.GetConnectionString();

        public async Task InitializeAsync()
        {
            await _postgres.StartAsync();
        }

        public async Task DisposeAsync()
        {
            await _postgres.DisposeAsync();
        }
    }
}
