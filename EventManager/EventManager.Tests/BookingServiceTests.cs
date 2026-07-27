using Castle.Core.Logging;
using EventManager.BackgroundServices;
using EventManager.DTOs.Events;
using EventManager.Exceptions;
using EventManager.Interfaces;
using EventManager.Models;
using EventManager.Services;
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

        private async Task<EventInfoDTO> CreateEventAsync()
        {
            return await _eventService.CreateEventAcync(new CreateEventDTO
            {
                Title = "Event 1",
                StartAt = DateTime.Now,
                EndAt = DateTime.Now.AddHours(1),
                TotalSeats = 10
            });
        }

        // Тест проверяет создание брони для существующего события
        [Fact]
        public async Task CreateBookingAsync_ShouldCreateBookingWithPendingStatus()
        {
            //Arrange
            var newEvent = CreateEventAsync().Result;

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
            var newEvent = CreateEventAsync().Result;

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
            var newEvent = CreateEventAsync().Result;
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
            var newEvent = CreateEventAsync().Result;
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
            var createdEvent = CreateEventAsync().Result;
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
            var logger = new Mock<ILogger<BookingProcessingService>>();

            var service = new BookingProcessingService(bookingService.Object, logger.Object);

            //Act
            await service.ProcessPendingBookingAsync(CancellationToken.None);

            //Assert
            Assert.Equal(BookingStatus.Confirmed, booking.Status);
            Assert.NotNull(booking.ProcessedAt);

            bookingService.Verify(x => x.UpdateBookingAsync(It.IsAny<Booking>()), Times.Once);
        }
    }
}
