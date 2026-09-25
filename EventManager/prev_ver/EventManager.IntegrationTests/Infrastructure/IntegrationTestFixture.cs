using EventManager.Infrastructure.DataAccess;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace EventManager.IntegrationTests.Infrastructure
{
    public class IntegrationTestFixture : IAsyncLifetime
    {
        private readonly PostgreSqlContainer _postgres;
        public WebApplicationFactory<Program> Factory { get; private set; } = null!;

        public string ConnectionString => _postgres.GetConnectionString();

        public IntegrationTestFixture()
        {
            _postgres = new PostgreSqlBuilder()
                .WithImage("postgres:16-alpine")
                .WithDatabase("eventapi_test")
                .WithUsername("postgres")
                .WithPassword("postgres")
                .Build();
        }
        public async Task InitializeAsync()
        {
            await _postgres.StartAsync();
            Factory = new CustomWebApplicationFactory(ConnectionString);
            using var scope = Factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await context.Database.MigrateAsync();
        }

        public async Task DisposeAsync()
        {
            if (Factory != null)
            {
                await Factory.DisposeAsync();
            }

            await _postgres.DisposeAsync();
        }
    }
}
