using EventManager.DataAccess;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Testcontainers.PostgreSql;

namespace EventManager.IntegrationTests
{
    [Collection("Postgres")]
    public class BookingRepositoryTests
    {
        private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("eventapi_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

        public async Task InitializeAsync()
        {
            await _postgres.StartAsync();
        }

        public async Task DisposeAsync()
        {
            await _postgres.DisposeAsync();
        }

        private AppDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql(_postgres.GetConnectionString())
                .Options;

            var context = new AppDbContext(options);
            context.Database.EnsureCreated();
            return context;
        }

        private async Task ResetDatabaseAsync()
        {
            await using var context = CreateContext();
            await context.Database.EnsureDeletedAsync();
        }
    }
}
