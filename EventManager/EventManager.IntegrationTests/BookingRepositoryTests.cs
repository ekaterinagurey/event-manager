using Docker.DotNet.Models;
using EventManager.DataAccess;
using EventManager.Models;
using EventManager.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Testcontainers.PostgreSql;

namespace EventManager.IntegrationTests
{
    [Collection("Postgres")]
    public class BookingRepositoryTests : IAsyncLifetime
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
                .UseNpgsql(_postgres.GetConnectionString(),
                            npgsqlOptions =>
                            {
                                npgsqlOptions.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                            })
                .Options;

            return new AppDbContext(options);
        }

        private async Task ResetDatabaseAsync()
        {
            await using var context = CreateContext();
            await context.Database.EnsureDeletedAsync();
            await context.Database.MigrateAsync();
        }

            #region GetByIdAsync Tests
            //Тест на получение бронирования
            [Fact]
        public async Task GetByIdAsync_ReturnsCorrectBooking()
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

            var booking = Booking.Create(newEvent.Id);
            arrangeContext.Bookings.AddAsync(booking);
            await arrangeContext.SaveChangesAsync();

            // Act
            await using var actContext = CreateContext();
            var repository = new BookingRepository(actContext);
            var result = await repository.GetByIdAsync(booking.Id, default);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(newEvent.Id, result.EventId);
        }

        //Тест на получение отсутствующего бронирования
        [Fact]
        public async Task GetByIdAsync_ReturnNull_WhenBookingDoesNotExist()
        {
            await ResetDatabaseAsync();

            // Arrange
            await using var context = CreateContext();
            var repository = new BookingRepository(context);

            // Act & Assert
            var result = await repository.GetByIdAsync(Guid.NewGuid(), default);
            Assert.Null(result);
        }
        #endregion

        #region GetPendingAsync Tests
        //Тест на получение бронирования
        [Fact]
        public async Task GetPendingAsync_ReturnsCorrectBooking()
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

            var booking = Booking.Create(newEvent.Id);
            arrangeContext.Bookings.AddAsync(booking);
            await arrangeContext.SaveChangesAsync();

            // Act
            await using var actContext = CreateContext();
            var repository = new BookingRepository(actContext);
            var result = await repository.GetPendingAsync(default);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.All(x => x.Status == BookingStatus.Pending));
        }
        #endregion

        #region CreateAsync Tests

        //Тест на создание бронирования
        [Fact]
        public async Task CreateAsync_SavesBookingToDatabase()
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
            var repository = new BookingRepository(actContext);
            var booking = Booking.Create(newEvent.Id);
            await repository.CreateAsync(booking, default);

            // Assert
            await using var verifyContext = CreateContext();
            var result = await verifyContext.Bookings.FirstOrDefaultAsync(e => e.Id == booking.Id);

            Assert.NotNull(result);
            Assert.Equal(booking.EventId, result.EventId);
            Assert.Equal(booking.Id, result.Id);
        }
        #endregion

        #region UpdateAsync Tests

        //Тест изменение бронирования
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

            var booking = Booking.Create(newEvent.Id);
            arrangeContext.Bookings.AddAsync(booking);
            await arrangeContext.SaveChangesAsync();

            // Act
            await using var actContext = CreateContext();
            var repository = new BookingRepository(actContext);
            booking.Confirm();
            await repository.UpdateAsync(booking, default);

            // Assert
            await using var verifyContext = CreateContext();
            var updated = await verifyContext.Bookings.FirstAsync(b => b.Id == booking.Id);
            Assert.Equal(BookingStatus.Confirmed, updated.Status);
            Assert.NotNull(updated.ProcessedAt);
        }
        #endregion
    }
}
