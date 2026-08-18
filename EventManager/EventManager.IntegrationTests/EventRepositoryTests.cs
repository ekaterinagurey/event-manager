using Docker.DotNet.Models;
using EventManager.DataAccess;
using EventManager.Models;
using EventManager.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Testcontainers.PostgreSql;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace EventManager.IntegrationTests
{
    public class EventRepositoryTests : IAsyncLifetime
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

        private async Task AddTestEventsAsync(int count = 5)
        {
            await using var context = CreateContext();

            var events = Enumerable.Range(1, count)
                .Select(i => Event.Create($"Test Event {i}",
                                          DateTime.UtcNow.AddDays(i),
                                          DateTime.UtcNow.AddDays(i).AddHours(1),
                                          10))
                .ToList();

            context.Events.AddRange(events);
            await context.SaveChangesAsync();
        }

        #region CreateAsync Tests

        //Тест на создание события
        [Fact]
        public async Task CreateAsync_SavesEventToDatabase()

        {
            await ResetDatabaseAsync();

            // Arrange
            var newEvent = Event.Create("Test event",
                                      DateTime.UtcNow,
                                      DateTime.UtcNow.AddHours(1),
                                      10);

            // Act
            var repository = new EventRepository(CreateContext());
            await repository.CreateAsync(newEvent, default);

            // Assert
            await using var verifyContext = CreateContext();
            var result = await verifyContext.Events.FirstOrDefaultAsync(e => e.Id == newEvent.Id);

            Assert.NotNull(result);
            Assert.Equal("Test event", result.Title);
            Assert.Equal(10, result.TotalSeats);
            Assert.Equal(10, result.AvailableSeats);

        }

        #endregion

        #region GetByIdAsync Tests
        //Тест на получение события
        [Fact]
        public async Task GetByIdAsync_ReturnsCorrectEvent()
        {
            await ResetDatabaseAsync();

            // Arrange
            await using var arrangeContext = CreateContext();

            var newEvent = Event.Create("Test event",
                                        DateTime.UtcNow,
                                        DateTime.UtcNow.AddHours(1),
                                        10);

            arrangeContext.Events.Add(newEvent);
            await arrangeContext.SaveChangesAsync();

            // Act
            var repository = new EventRepository(CreateContext());
            var result = await repository.GetByIdAsync(newEvent.Id, default);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test event", result.Title);
        }

        //Тест на получение отсутствующего события
        [Fact]
        public async Task GetByIdAsync_ReturnNull_WhenEventDoesNotExist()
        {
            await ResetDatabaseAsync();

            // Arrange
            await using var context = CreateContext();
            var repository = new EventRepository(context);

            // Act & Assert
            var result = await repository.GetByIdAsync(Guid.NewGuid(), default);
            Assert.Null(result);
        }

        #endregion

        #region GetPagedAsync Tests
        // Тест на получение списка событий с пагинацией. Без фильтров
        [Fact]
        public async Task GetPagedAsync_ReturnCorrectPage()
        {
            await ResetDatabaseAsync();

            // Arrange
            await AddTestEventsAsync(15);

            // Act
            var repository = new EventRepository(CreateContext());
            var result = await repository.GetPagedAsync(null, null, null, 2, 5);

            // Assert
            Assert.Equal(15, result.TotalCount);
            Assert.Equal(5, result.Events.Count);
        }

        // Тест на получение списка событий с пагинацией. Фильтр по названию
        [Fact]
        public async Task GetPagedAsync_ReturnCorrectEvent_FilterTitle()
        {
            await ResetDatabaseAsync();

            // Arrange
            await AddTestEventsAsync(15);

            // Act
            var repository = new EventRepository(CreateContext());
            var result = await repository.GetPagedAsync("Test event 1", null, null, 1, 5);

            // Assert
            Assert.Equal(7, result.TotalCount);
            Assert.Equal(5, result.Events.Count);
        }

        // Тест на получение списка событий с пагинацией. Фильтр по дате 'From'
        [Fact]
        public async Task GetPagedAsync_ReturnCorrectEvent_FilterFrom()
        {
            await ResetDatabaseAsync();

            // Arrange
            await AddTestEventsAsync(15);

            // Act
            var repository = new EventRepository(CreateContext());
            var result = await repository.GetPagedAsync(null, DateTime.UtcNow.AddDays(3), null, 1, 5);

            // Assert
            Assert.Equal(12, result.TotalCount);
            Assert.Equal(5, result.Events.Count);
        }

        // Тест на получение списка событий с пагинацией. Фильтр по дате 'To'
        [Fact]
        public async Task GetPagedAsync_ReturnCorrectEvent_FilterTo()
        {
            await ResetDatabaseAsync();

            // Arrange
            await AddTestEventsAsync(15);

            // Act
            var repository = new EventRepository(CreateContext());
            var result = await repository.GetPagedAsync(null, null, DateTime.UtcNow.AddDays(3), 1, 5);

            // Assert
            Assert.Equal(2, result.TotalCount);
            Assert.Equal(2, result.Events.Count);
        }

        // Тест на получение списка событий с пагинацией. Фильтр интервалу дат 
        [Fact]
        public async Task GetPagedAsync_ReturnCorrectEvent_FilterFromTo()
        {
            await ResetDatabaseAsync();

            // Arrange
            await AddTestEventsAsync(15);

            // Act
            var repository = new EventRepository(CreateContext());
            var result = await repository.GetPagedAsync(null,
                                                        DateTime.UtcNow.AddDays(1),
                                                        DateTime.UtcNow.AddDays(8),
                                                        1,
                                                        5);

            // Assert
            Assert.Equal(6, result.TotalCount);
            Assert.Equal(5, result.Events.Count);
        }

        // Тест на получение списка событий с пагинацией. Комбинированный фильтр
        [Fact]
        public async Task GetPagedAsync_ReturnCorrectEvent_СombinationFilter()
        {
            await ResetDatabaseAsync();

            // Arrange
            await AddTestEventsAsync(15);

            // Act
            var repository = new EventRepository(CreateContext());
            var result = await repository.GetPagedAsync("Test Event 1",
                                                        DateTime.UtcNow.AddDays(7),
                                                        DateTime.UtcNow.AddDays(13),
                                                        1,
                                                        5);

            // Assert
            Assert.Equal(3, result.TotalCount);
            Assert.Equal(3, result.Events.Count);
        }


        #endregion

        #region UpdateAsync Tests
        // Тест на обновление события
        [Fact]
        public async Task UpdateAsync_ChangesFieldInDatabase()
        {
            await ResetDatabaseAsync();

            // Arrange
            await using var arrangeContext = CreateContext();

            var newEvent = Event.Create("Test event",
                                        DateTime.UtcNow,
                                        DateTime.UtcNow.AddHours(1),
                                        10);

            arrangeContext.Events.Add(newEvent);
            await arrangeContext.SaveChangesAsync();

            // Act
            await using var actContext = CreateContext();
            newEvent.Update("Новое название",
                            newEvent.StartAt,
                            newEvent.EndAt,
                            newEvent.Description);

            actContext.Events.Update(newEvent);
            await actContext.SaveChangesAsync();

            // Assert
            await using var verifyContext = CreateContext();
            var updated = await verifyContext.Events.FirstAsync(e => e.Id == newEvent.Id);
            Assert.Equal("Новое название", updated.Title);
        }
        #endregion

        #region DeleteAsync Tests
        //Тест на удаление события
        [Fact]
        public async Task DeleteAsync_RemovesFromDatabase()
        {
            await ResetDatabaseAsync();

            // Arrange
            await using var context = CreateContext();

            var newEvent = Event.Create("Удаляемое",
                                        DateTime.UtcNow,
                                        DateTime.UtcNow.AddHours(1),
                                        10);

            context.Events.Add(newEvent);
            await context.SaveChangesAsync();

            // Act
            await using var actContext = CreateContext();
            var existEvent = await actContext.Events.FirstAsync(e => e.Id == newEvent.Id);
            actContext.Events.Remove(existEvent);
            await actContext.SaveChangesAsync();

            // Assert
            await using var verifyContext = CreateContext();
            var deleted = await verifyContext.Events.FirstOrDefaultAsync(e => e.Id == newEvent.Id);
            Assert.Null(deleted);
        }
        #endregion

    }
}
