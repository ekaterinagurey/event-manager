using EventManager.BackgroundServices;
using EventManager.DTOs.Events;
using EventManager.Exceptions;
using EventManager.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using EventManager.Services;
using EventManager.DataAccess;
using Moq;
using Microsoft.EntityFrameworkCore;
using EventManager.Services.Interfaces;

namespace EventManager.Tests
{
    public sealed class BookingServiceTests : IDisposable
    {
        private readonly ServiceProvider _serviceProvider;
        private readonly IServiceScope _scope;
        private readonly IEventService _eventService;
        private readonly IBookingService _bookingService;

        public BookingServiceTests()
        {
            var dbName = Guid.NewGuid().ToString();
            var services = new ServiceCollection();
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(dbName));
            services.AddScoped<IEventService, EventService>();
            services.AddScoped<IBookingService, BookingService>();
            services.AddLogging();

            _serviceProvider = services.BuildServiceProvider();
            _scope = _serviceProvider.CreateScope();
            _eventService = _scope.ServiceProvider.GetRequiredService<IEventService>();
            _bookingService = _scope.ServiceProvider.GetRequiredService<IBookingService>();
        }

        public void Dispose()
        {
            _scope.Dispose();
            _serviceProvider.Dispose();
        }

        private async Task<EventInfoDTO> CreateTestEventAsync(int totalSeats = 10)
        {
            return await _eventService.CreateEventAsync(new CreateEventDTO
            {
                Title = "Event 1",
                StartAt = DateTime.Now,
                EndAt = DateTime.Now.AddHours(1),
                TotalSeats = totalSeats
            });
        }

        #region CreateBookingAsync Tests

        // Тест проверяет создание брони для существующего события
        [Fact]
        public async Task CreateBookingAsync_ShouldCreateBookingWithPendingStatus()
        {
            //Arrange
            var newEvent = await CreateTestEventAsync();

            //Act
            var booking = await _bookingService.CreateBookingAsync(newEvent.Id);

            //Assert
            Assert.NotNull(booking);
            Assert.Equal(newEvent.Id, booking.EventId);
            Assert.Equal(BookingStatus.Pending, booking.Status);
            Assert.NotEqual(Guid.Empty, booking.Id);
        }

        // Тест проверяет создание нескольких броней для одного события
        [Fact]
        public async Task CreateBookingAsync_ShouldCreateMultipleBookingsForOneEvent()
        {
            //Arrange
            var newEvent = await CreateTestEventAsync();

            //Act
            var booking1 = await _bookingService.CreateBookingAsync(newEvent.Id);
            var booking2 = await _bookingService.CreateBookingAsync(newEvent.Id);

            //Assert
            Assert.NotEqual(booking1.Id, booking2.Id);
        }

        // Тест проверяет создание брони для несуществующего события
        [Fact]
        public async Task CreateBookingAsync_ShouldThrow_WhenEvenDoesNotExist()
        {
            //Arrange
            var eventId = Guid.NewGuid();

            //Act && Assert
            await Assert.ThrowsAsync<NotFoundException>(() => _bookingService.CreateBookingAsync(eventId));
        }

        // Тест проверяет создание брони для удалённого события
        [Fact]
        public async Task CreateBookingAsync_ShouldThrow_WhenEvenWasRemoved()
        {
            //Arrange
            var createdEvent = await CreateTestEventAsync();
            await _eventService.RemoveEventAsync(createdEvent.Id);

            //Act && Assert
            await Assert.ThrowsAsync<NotFoundException>(() => _bookingService.CreateBookingAsync(createdEvent.Id));
        }

        //Тест проверяет, что создание брони уменьшает AvailableSeats на 1
        [Fact]
        public async Task CreateBookingAsync_ShouldDecreaseAvailableSeats()
        {
            //Arrange
            var newEvent = await CreateTestEventAsync(5);

            //Act
            await _bookingService.CreateBookingAsync(newEvent.Id);

            //Assert
            var updatedEvent = await _eventService.GetEventByIdAsync(newEvent.Id);
            Assert.Equal(4, updatedEvent?.AvailableSeats);
        }

        //Тест проверяет, что создание нескольких броней (до лимита) — все успешны, у каждой уникальный Id
        [Fact]
        public async Task CreateBookingAsync_ShouldCreateBookingsUntilLimit()
        {
            //Arrange
            var newEvent = await CreateTestEventAsync(3);

            //Act
            var bookings = new List<Booking>();
            for (int i = 0; i < 3; i++)
            {
                bookings.Add(await _bookingService.CreateBookingAsync(newEvent.Id));
            }

            //Assert
            Assert.Equal(3, bookings.Count);
            Assert.Equal(3, bookings.Select(x => x.Id).Distinct().Count());
            var updatedEvent = await _eventService.GetEventByIdAsync(newEvent.Id);
            Assert.Equal(0, updatedEvent?.AvailableSeats);
        }

        //Тест на бронирование для несуществующего события
        [Fact]
        public async Task CreateBookingAsync_ShouldThrow_WhenEventNotExist()
        {
            await Assert.ThrowsAsync<NotFoundException>(() => _bookingService.CreateBookingAsync(Guid.NewGuid()));
        }

        //Тест проверяет, что бронирование при отсутствии мест выбрасывает исключение NoAvailableSeatsException
        [Fact]
        public async Task CreateBookingAsync_ShouldThrow_WhenNoSeatsAvailable()
        {
            //Arrange
            var newEvent = await CreateTestEventAsync(1);
            await _bookingService.CreateBookingAsync(newEvent.Id);

            //Act && Assert
            await Assert.ThrowsAsync<NoAvailableSeatsException>(() => _bookingService.CreateBookingAsync(newEvent.Id));
        }

        //Тест на защиту от овербукинга
        [Fact]
        public async Task CreateBookingAsync_ShouldPreventOverbooking()
        {
            //Arrange
            var newEvent = await CreateTestEventAsync(5);

            //Act
            var tasks = Enumerable.Range(0, 20)
                .Select(async _ =>
                {
                    try
                    {
                        return await _bookingService.CreateBookingAsync(newEvent.Id);

                    }
                    catch (NoAvailableSeatsException)
                    {
                        return null;
                    }
                });

            var bookings = await Task.WhenAll(tasks);

            //Assert
            Assert.Equal(5, bookings.Count(x => x != null));
            Assert.Equal(15, bookings.Count(x => x == null));

            var entity = await _eventService.GetEventByIdAsync(newEvent.Id);
            Assert.Equal(0, entity?.AvailableSeats);
        }

        //Тест на уникальность Id при конкурентных запросах
        [Fact]
        public async Task CreateBookingAsync_ShouldCreateUniqueIds()
        {
            //Arrange
            var newEvent = await CreateTestEventAsync(10);

            //Act
            var tasks = Enumerable.Range(0, 10)
                .Select(async _ => await _bookingService.CreateBookingAsync(newEvent.Id));

            var bookings = await Task.WhenAll(tasks);

            //Assert
            Assert.Equal(10, bookings.Length);
            Assert.Equal(10, bookings.Select(x => x.Id).Distinct().Count());
        }

        #endregion

        #region GetBookingByIdAsync Tests

        // Тест проверяет получение брони по Id
        [Fact]
        public async Task GetBookingByIdAsync_ShouldReturnBooking()
        {
            //Arrange
            var newEvent = await CreateTestEventAsync();
            var createdBooking = await _bookingService.CreateBookingAsync(newEvent.Id);

            //Act
            var booking = await _bookingService.GetBookingByIdAsync(createdBooking.Id);


            //Assert
            Assert.Equal(createdBooking.Id, booking.Id);
            Assert.Equal(createdBooking.EventId, booking.EventId);
            Assert.Equal(createdBooking.Status, booking.Status);
            Assert.Equal(createdBooking.CreatedAt, booking.CreatedAt);
        }

        // Тест проверяет, что получение брони отражает изменение статуса
        [Fact]
        public async Task GetBookingByIdAsync_ShouldReturnUpdatedStatus()
        {
            //Arrange
            var newEvent = await CreateTestEventAsync();
            var createdBooking = await _bookingService.CreateBookingAsync(newEvent.Id);

            //Act
            createdBooking.Confirm();

            //Assert
            Assert.Equal(BookingStatus.Confirmed, createdBooking.Status);
            Assert.NotNull(createdBooking.ProcessedAt);
        }

        // Тест проверяет получение брони по несуществующему Id
        [Fact]
        public async Task GetBookingByIdAsync_ShouldThrow_WhenBookingDoesNotExist()
        {
            //Arrange
            var bookingId = Guid.NewGuid();

            //Act && Assert
            await Assert.ThrowsAsync<NotFoundException>(() => _bookingService.GetBookingByIdAsync(bookingId));
        }

        #endregion

        #region ProcessPendingBookingAsync Tests
        // Тест проверяет фоновую обработку бронирований в состоянии pending

        [Fact]
        public async Task ProcessBookingAsync_ShouldConfirmPendingBooking()
        {
            // Arrange
            Guid bookingId;
            Guid eventId;

            // Создаём Event и Pending Booking
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<AppDbContext>();

                var createdEvent = Event.Create(
                    "Test event",
                    DateTime.Now,
                    DateTime.Now.AddHours(1),
                    5);

                context.Events.Add(createdEvent);

                var booking = Booking.Create(createdEvent.Id);

                context.Bookings.Add(booking);

                await context.SaveChangesAsync();

                eventId = createdEvent.Id;
                bookingId = booking.Id;
            }

            // Создаём BookingProcessingService
            var scopeFactory = _serviceProvider
                .GetRequiredService<IServiceScopeFactory>();

            var logger = _serviceProvider
                .GetRequiredService<ILogger<BookingProcessingService>>();

            var processingService = new BookingProcessingService(
                scopeFactory,
                logger);

            // Act
            await processingService.ProcessBookingAsync(
                bookingId,
                default);

            // Assert
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider
                    .GetRequiredService<AppDbContext>();

                var booking = await context.Bookings
                    .FirstOrDefaultAsync(b => b.Id == bookingId);

                Assert.NotNull(booking);
                Assert.Equal(BookingStatus.Confirmed, booking.Status);
                Assert.NotNull(booking.ProcessedAt);
            }
        }

        #endregion

        #region Confirm & Reject Tests
        //Тест проверяет, что после вызова Confirm() бронь возвращает статус Confirmed и заполненный ProcessedAt
        [Fact]
        public async Task Confirm_ShouldChangeStatus()
        {
            //Arrange
            var booking = Booking.Create(Guid.NewGuid());

            //Act
            booking.Confirm();

            //Assert
            Assert.Equal(BookingStatus.Confirmed, booking.Status);
            Assert.NotNull(booking.ProcessedAt);
        }

        //Тест проверяет, что после вызова Reject() бронь возвращает статус Rejected и заполненный ProcessedAt
        [Fact]
        public void Reject_ShouldChangeStatus()
        {
            //Arrange
            var booking = Booking.Create(Guid.NewGuid());

            //Act
            booking.Reject();

            //Assert
            Assert.Equal(BookingStatus.Rejected, booking.Status);
            Assert.NotNull(booking.ProcessedAt);
        }

        //Тест проверяет, что после ReleaseSeats() количество свободных мест восстанавливается
        [Fact]
        public async Task Reject_ShouldRestorevailableSeats()
        {
            //Arrange
            var newEvent = await CreateTestEventAsync(5);
            var booking = await _bookingService.CreateBookingAsync(newEvent.Id);

            //Act && Assert
            var updatedEvent = await _eventService.GetEventByIdAsync(newEvent.Id);
            Assert.Equal(4, updatedEvent?.AvailableSeats);

            updatedEvent?.ReleaseSeats();
            Assert.Equal(5, updatedEvent?.AvailableSeats);
        }

        //Тест проверяет, что после ReleaseSeats() можно успешно создать новую бронь на то же место
        [Fact]
        public async Task Reject_ShouldAllowNewBooking()
        {
            //Arrange
            var newEvent = await CreateTestEventAsync(1);
            var booking = await _bookingService.CreateBookingAsync(newEvent.Id);

            //Act && Assert
            booking.Reject();
            var entity = await _eventService.GetEventByIdAsync(newEvent.Id);
            entity?.ReleaseSeats();

            var secondBooking = await _bookingService.CreateBookingAsync(newEvent.Id);
            Assert.NotNull(secondBooking);
            Assert.Equal(newEvent.Id, secondBooking.EventId);
        }

        #endregion
    }
}
