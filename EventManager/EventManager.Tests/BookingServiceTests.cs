using Castle.Core.Logging;
using EventManager.BackgroundServices;
using EventManager.DTOs.Events;
using EventManager.Exceptions;
using EventManager.Interfaces;
using EventManager.Mappers;
using EventManager.Models;
using EventManager.Services;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventManager.Tests
{
    public class BookingServiceTests
    {
        private readonly EventService _eventService;
        private readonly BookingService _bookingService;

        public BookingServiceTests()
        {
            _eventService = new EventService();
            _bookingService = new BookingService(_eventService);
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

            createdBooking.Status = BookingStatus.Confirmed;
            createdBooking.ProcessedAt = DateTime.Now;

            await _bookingService.UpdateBookingAsync(createdBooking);

            //Act
            var booking = await _bookingService.GetBookingByIdAsync(createdBooking.Id);

            //Assert
            Assert.Equal(BookingStatus.Confirmed, booking.Status);
            Assert.NotNull(booking.ProcessedAt);
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
            _eventService.RemoveEvent(createdEvent.Id);

            //Act && Assert
            await Assert.ThrowsAsync<NotFoundException>(() => _bookingService.CreateBookingAsync(createdEvent.Id));
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

        // Тест проверяет фоновую обработку бронирований в состоянии pending
        [Fact]
        public async Task ProcessPendingBookingAsync_ShouldConfirmPendingBooking()
        {
            //Arrange
            var booking = new Booking
            {
                Id = Guid.NewGuid(),
                Status = BookingStatus.Pending,
                CreatedAt = DateTime.Now
            };

            var bookingService = new Mock<IBookingService>();
            bookingService.Setup(x => x.GetPendingBookingAsync()).ReturnsAsync(new[] { booking });
            bookingService.Setup(x => x.UpdateBookingAsync(It.IsAny<Booking>())).Returns(Task.CompletedTask);

            var eventService = new Mock<IEventService>();
            eventService.Setup(x => x.GetEvent(booking.EventId)).Returns(new Event { Id = booking.Id,
                                                                                     Title = "Test",
                                                                                     StartAt = DateTime.Now,
                                                                                     EndAt = DateTime.Now.AddHours(1),
                                                                                     TotalSeats = 10,
                                                                                     AvailableSeats = 9});
            var logger = new Mock<ILogger<BookingProcessingService>>();

            var service = new BookingProcessingService(bookingService.Object, eventService.Object, logger.Object);

            //Act
            await service.ProcessBookingAsync(booking, CancellationToken.None);

            //Assert
            Assert.Equal(BookingStatus.Confirmed, booking.Status);
            Assert.NotNull(booking.ProcessedAt);

            bookingService.Verify(x => x.UpdateBookingAsync(It.IsAny<Booking>()), Times.Once);
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
            var updatedEvent = _eventService.GetEvent(newEvent.Id);
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
            var updatedEvent = _eventService.GetEvent(newEvent.Id);
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

        //Тест проверяет, что после вызова Confirm() бронь возвращает статус Confirmed и заполненный ProcessedAt
        [Fact]
        public async Task Confirm_ShouldChangeStatus()
        {
            //Arrange
            var booking = new Booking { Id = Guid.NewGuid(),
                                        EventId = Guid.NewGuid(),
                                        Status = BookingStatus.Pending,
                                        CreatedAt = DateTime.Now};

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
            var booking = new Booking
            {
                Id = Guid.NewGuid(),
                EventId = Guid.NewGuid(),
                Status = BookingStatus.Pending,
                CreatedAt = DateTime.Now
            };

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
            var updatedEvent = _eventService.GetEvent(newEvent.Id);
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
            var entity = _eventService.GetEvent(newEvent.Id);
            entity?.ReleaseSeats();

            var secondBooking = await _bookingService.CreateBookingAsync(newEvent.Id);
            Assert.NotNull(secondBooking);
            Assert.Equal(newEvent.Id, secondBooking.EventId);
        }
    }
}
