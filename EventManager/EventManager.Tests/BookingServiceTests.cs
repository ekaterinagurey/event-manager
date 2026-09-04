using EventManager.Application.BackgroundServices;
using EventManager.Application.DTOs.Events;
using EventManager.Application.Interfaces.Authentication;
using EventManager.Application.Interfaces.Repositories;
using EventManager.Application.Services;
using EventManager.Application.Services.Interfaces;
using EventManager.Domain.Enums;
using EventManager.Domain.Exceptions;
using EventManager.Domain.Models;
using EventManager.Infrastructure.Authentication;
using EventManager.Infrastructure.DataAccess;
using EventManager.Infrastructure.Repositories;
using EventManager.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Data;

namespace EventManager.Tests
{
    public sealed class BookingServiceTests : IDisposable
    {
        private readonly ServiceProvider _serviceProvider;
        private readonly IServiceScope _scope;
        private readonly IEventService _eventService;
        private readonly IBookingService _bookingService;
        private readonly IUserService _userService;

        public BookingServiceTests()
        {
            var dbName = Guid.NewGuid().ToString();
            var services = new ServiceCollection();
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(dbName));
            services.AddScoped<IEventRepository, EventRepository>();
            services.AddScoped<IBookingRepository, BookingRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IEventService, EventService>();
            services.AddScoped<IBookingService, BookingService>();
            services.AddScoped<IPasswordHasher, PasswordHasher>();
            services.AddScoped<IJwtTokenService, JwtTokenService>();
            services.AddScoped<IUserService, UserService>();
            services.AddLogging();

            _serviceProvider = services.BuildServiceProvider();
            _scope = _serviceProvider.CreateScope();
            _eventService = _scope.ServiceProvider.GetRequiredService<IEventService>();
            _bookingService = _scope.ServiceProvider.GetRequiredService<IBookingService>();
            _userService = _scope.ServiceProvider.GetRequiredService<IUserService>();
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

        private async Task<User> CreateTestUserAsync()
        {
            var user = User.Create("testuser", "hashed_password", UserRole.User);
            await _userService.RegisterAsync(user.Login, user.PasswordHash, user.Role, default);
            return user;
        }

        #region CreateBookingAsync Tests

        // Тест проверяет создание брони для существующего события
        [Fact]
        public async Task CreateBookingAsync_ShouldCreateBookingWithPendingStatus()
        {
            //Arrange
            var newEvent = await CreateTestEventAsync();
            var userId = Guid.NewGuid();

            //Act
            var booking = await _bookingService.CreateBookingAsync(newEvent.Id, userId);

            //Assert
            Assert.NotNull(booking);
            Assert.Equal(newEvent.Id, booking.EventId);
            Assert.Equal(userId, booking.UserId);
            Assert.Equal(BookingStatus.Pending, booking.Status);
            Assert.NotEqual(Guid.Empty, booking.Id);
        }

        // Тест проверяет создание нескольких броней для одного события
        [Fact]
        public async Task CreateBookingAsync_ShouldCreateMultipleBookingsForOneEvent()
        {
            //Arrange
            var newEvent = await CreateTestEventAsync();
            var userId = Guid.NewGuid();

            //Act
            var booking1 = await _bookingService.CreateBookingAsync(newEvent.Id, userId);
            var booking2 = await _bookingService.CreateBookingAsync(newEvent.Id, userId);

            //Assert
            Assert.NotEqual(booking1.Id, booking2.Id);
        }

        // Тест проверяет создание брони для несуществующего события
        [Fact]
        public async Task CreateBookingAsync_ShouldThrow_WhenEvenDoesNotExist()
        {
            //Arrange
            var eventId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            //Act && Assert
            await Assert.ThrowsAsync<NotFoundException>(() => _bookingService.CreateBookingAsync(eventId, userId));
        }

        // Тест проверяет создание брони для удалённого события
        [Fact]
        public async Task CreateBookingAsync_ShouldThrow_WhenEvenWasRemoved()
        {
            //Arrange
            var createdEvent = await CreateTestEventAsync();
            await _eventService.RemoveEventAsync(createdEvent.Id);
            var userId = Guid.NewGuid();

            //Act && Assert
            await Assert.ThrowsAsync<NotFoundException>(() => _bookingService.CreateBookingAsync(createdEvent.Id, 
                                                                                                 userId));
        }

        //Тест проверяет, что создание брони уменьшает AvailableSeats на 1
        [Fact]
        public async Task CreateBookingAsync_ShouldDecreaseAvailableSeats()
        {
            //Arrange
            var newEvent = await CreateTestEventAsync(5);
            var userId = Guid.NewGuid();

            //Act
            await _bookingService.CreateBookingAsync(newEvent.Id, userId);

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
                bookings.Add(await _bookingService.CreateBookingAsync(newEvent.Id, Guid.NewGuid()));
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
            await Assert.ThrowsAsync<NotFoundException>(() => _bookingService.CreateBookingAsync(Guid.NewGuid(), Guid.NewGuid()));
        }

        //Тест проверяет, что бронирование при отсутствии мест выбрасывает исключение NoAvailableSeatsException
        [Fact]
        public async Task CreateBookingAsync_ShouldThrow_WhenNoSeatsAvailable()
        {
            //Arrange
            var newEvent = await CreateTestEventAsync(1);
            var userId = Guid.NewGuid();
            await _bookingService.CreateBookingAsync(newEvent.Id, userId);

            //Act && Assert
            await Assert.ThrowsAsync<NoAvailableSeatsException>(() => _bookingService.CreateBookingAsync(newEvent.Id, userId));
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
                        return await _bookingService.CreateBookingAsync(newEvent.Id, Guid.NewGuid());

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
            var userId = Guid.NewGuid();

            //Act
            var tasks = Enumerable.Range(0, 10)
                .Select(async _ => await _bookingService.CreateBookingAsync(newEvent.Id, userId));

            var bookings = await Task.WhenAll(tasks);

            //Assert
            Assert.Equal(10, bookings.Length);
            Assert.Equal(10, bookings.Select(x => x.Id).Distinct().Count());
        }

        //Тест, проверяющий, что попытка забронировать прошедшее событие приводит к ошибке
        [Fact]
        public async Task CreateBookingAsync_WhenEventIsInThePast_ShouldThrowException()
        {
            // Arrange
            var user = await CreateTestUserAsync();

            var pastEvent = await _eventService.CreateEventAsync(new CreateEventDTO
            {
                Title = "Event 1",
                StartAt = DateTime.UtcNow.AddDays(-2),
                EndAt = DateTime.UtcNow.AddDays(-1),
                TotalSeats = 10
            });
                
            // Act & Assert
            await Assert.ThrowsAsync<PastEventBookingException>(async () =>
            {
                await _bookingService.CreateBookingAsync(pastEvent.Id, user.Id, default);
            });
        }

        //Тест проверяющий, что при достижении лимита активных броней новая бронь не создаётся
        [Fact]
        public async Task CreateBookingAsync_WhenUserBookingLimitExceededException_ShouldThrowException()
        {
            // Arrange
            var user = await CreateTestUserAsync();
            const int maxActiveBookings = 10; 

            // Создаем и занимаем разрешенный максимум активных броней
            for (var i = 0; i < maxActiveBookings; i++)
            {
                var newEvent = await _eventService.CreateEventAsync(new CreateEventDTO
                {
                    Title = $"Event {i}",
                    StartAt = DateTime.UtcNow.AddDays(i + 1),
                    EndAt = DateTime.UtcNow.AddDays(i + 2),
                    TotalSeats = 10
                });

                await _bookingService.CreateBookingAsync(newEvent.Id, user.Id, default);
            }

            // Событие, на которое пользователь пробует забронировать сверх лимита
            var extraEvent = await _eventService.CreateEventAsync(new CreateEventDTO
            {
                Title = $"Extra event",
                StartAt = DateTime.UtcNow.AddDays(10),
                EndAt = DateTime.UtcNow.AddDays(11),
                TotalSeats = 10
            });

            // Act & Assert
            await Assert.ThrowsAsync<BookingLimitExceededException>(async () =>
            {
                await _bookingService.CreateBookingAsync(extraEvent.Id, user.Id, CancellationToken.None);
            });
        }

        //Тест проверяющий, что лимиты разных пользователей не влияют друг на друга
        [Fact]
        public async Task CreateBookingAsync_WhenOneUserReachesLimit_OtherUserCanStillBook()
        {
            // Arrange
            var user1 = User.Create("testuser1", "hashed_password", UserRole.User);
            await _userService.RegisterAsync(user1.Login, user1.PasswordHash, user1.Role, default);

            var user2 = User.Create("testuser2", "hashed_password", UserRole.User);
            await _userService.RegisterAsync(user2.Login, user2.PasswordHash, user2.Role, default);

            const int maxActiveBookings = 10;

            // Забиваем лимит первого пользователя User1
            for (var i = 0; i < maxActiveBookings; i++)
            {
                var newEvent = await _eventService.CreateEventAsync(new CreateEventDTO
                {
                    Title = $"Event {i}",
                    StartAt = DateTime.UtcNow.AddDays(i + 1),
                    EndAt = DateTime.UtcNow.AddDays(i + 2),
                    TotalSeats = 10
                });

                await _bookingService.CreateBookingAsync(newEvent.Id, user1.Id, default);
            }

            // Общее событие с достаточным количеством свободных мест
            var targetEvent = await _eventService.CreateEventAsync(new CreateEventDTO
            {
                Title = "Target event",
                StartAt = DateTime.UtcNow.AddDays(5),
                EndAt = DateTime.UtcNow.AddDays(6),
                TotalSeats = 10
            }); 
            
            // Act
            await Assert.ThrowsAsync<BookingLimitExceededException>(async () =>
            {
                await _bookingService.CreateBookingAsync(targetEvent.Id, user1.Id, default);
            });

            var user2Booking = await _bookingService.CreateBookingAsync(targetEvent.Id, user2.Id, default);

            // Assert
            Assert.NotNull(user2Booking);
            Assert.Equal(user2.Id, user2Booking.UserId);
            Assert.Equal(targetEvent.Id, user2Booking.EventId);
            Assert.Equal(BookingStatus.Pending, user2Booking.Status);
        }

        #endregion

        #region GetBookingByIdAsync Tests

        // Тест проверяет получение брони по Id
        [Fact]
        public async Task GetBookingByIdAsync_ShouldReturnBooking()
        {
            //Arrange
            var newEvent = await CreateTestEventAsync();
            var userId = Guid.NewGuid();
            var createdBooking = await _bookingService.CreateBookingAsync(newEvent.Id, userId);

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
            var userId = Guid.NewGuid();
            var createdBooking = await _bookingService.CreateBookingAsync(newEvent.Id, userId);

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

                var booking = Booking.Create(createdEvent.Id, Guid.NewGuid());

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
        public void Confirm_ShouldChangeStatus()
        {
            //Arrange
            var booking = Booking.Create(Guid.NewGuid(), Guid.NewGuid());

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
            var booking = Booking.Create(Guid.NewGuid(), Guid.NewGuid());

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
            var userId = Guid.NewGuid();
            var booking = await _bookingService.CreateBookingAsync(newEvent.Id, userId);

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
            var userId = Guid.NewGuid();
            var booking = await _bookingService.CreateBookingAsync(newEvent.Id, userId);

            //Act && Assert
            booking.Reject();
            var entity = await _eventService.GetEventByIdAsync(newEvent.Id);
            entity?.ReleaseSeats();

            var secondBooking = await _bookingService.CreateBookingAsync(newEvent.Id, userId);
            Assert.NotNull(secondBooking);
            Assert.Equal(newEvent.Id, secondBooking.EventId);
        }

        #endregion
    }
}
