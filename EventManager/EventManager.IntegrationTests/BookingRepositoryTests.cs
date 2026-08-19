using EventManager.DataAccess;
using EventManager.Exceptions;
using EventManager.IntegrationTests.Infrastructure;
using EventManager.Models;
using EventManager.Repositories;
using EventManager.Services;
using EventManager.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;

namespace EventManager.IntegrationTests
{
    [Collection("Postgres")]
    public class BookingRepositoryTests
    {
        private readonly PostgresFixture _fixture;

        public BookingRepositoryTests(PostgresFixture fixture)
        {
            _fixture = fixture;
        }

        private AppDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql(_fixture.ConnectionString,
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
            await arrangeContext.Bookings.AddAsync(booking);
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
            await arrangeContext.Bookings.AddAsync(booking);
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

        //Тест на создание бронирования с несуществующим event_id
        [Fact]
        public async Task CreateAsync_WithNonExistentEventId()
        {
            await ResetDatabaseAsync();

            // Arrange
            var booking = Booking.Create(Guid.NewGuid());

            // Act && Assert
            await using var context = CreateContext();
            var repository = new BookingRepository(context);
            await Assert.ThrowsAsync<DbUpdateException>(() => repository.CreateAsync(booking, default));
        }

        //Тест проверяет, что создание брони уменьшает AvailableSeats на 1
        [Fact]
        public async Task CreateAsync_ShouldDecreaseAvailableSeats()
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
            var bookingRepository = new BookingRepository(actContext);
            var eventRepository = new EventRepository(actContext);
            var bookingService = new BookingService(bookingRepository, eventRepository);
           
            var booking = await bookingService.CreateBookingAsync(newEvent.Id);

            //Assert
            await using var verifyContext = CreateContext();
            var updated = await verifyContext.Events.FirstAsync(b => b.Id == newEvent.Id);
            Assert.Equal(9, updated.AvailableSeats);
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
            await arrangeContext.Bookings.AddAsync(booking);
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
