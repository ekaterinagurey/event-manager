using EventManager.DTOs;
using EventManager.DTOs.Events;
using EventManager.Exceptions;
using EventManager.Mappers;
using EventManager.Models;
using EventManager.Services;

namespace EventManager.Tests
{
    public class EventServiceTests
    {
        private readonly EventService _service;

        public EventServiceTests()
        {
            _service = new EventService();
        }

        // Тест проверяет, что метод корректно создает событие
        [Fact]
        public async Task CreateEventAsync_ShouldCreateEvents()
        {
            //Arrange
            var newEvent = new CreateEventDTO
            {
                Title = "New Event",
                StartAt = new DateTime(2026, 7, 23),
                EndAt = new DateTime(2026, 7, 24),
                TotalSeats = 1
            };

            //Act
            var created = await _service.CreateEventAsync(newEvent);

            //Assert
            Assert.Equal(newEvent.Title, created.Title);
            Assert.Equal(newEvent.StartAt, created.StartAt);
            Assert.Equal(newEvent.EndAt, created.EndAt);
        }

        // Тест проверяет, что метод возвращает все события
        [Fact]
        public async Task GetEvents_ShouldReturnAllEvents()
        {
            //Arrange
            await _service.CreateEventAsync(new CreateEventDTO
            {
                Title = "Event1",
                StartAt = DateTime.Now,
                EndAt = DateTime.Now.AddHours(1),
                TotalSeats = 1
            });

            //Act
            var events = _service.GetEvents(new GetEventsRequestDTO());

            //Assert
            Assert.Single(events.Items);
        }

        // Тест проверяет, что метод возвращает событие по Id
        [Fact]
        public async Task GetEvent_ShouldReturnEvent()
        {
            //Arrange
            var created = await _service.CreateEventAsync(new CreateEventDTO
            {
                Title = "Event1",
                StartAt = DateTime.Now,
                EndAt = DateTime.Now.AddHours(1),
                TotalSeats = 1
            });

            //Act
            var result = _service.GetEvent(created.Id);

            //Assert
            Assert.Equal(created.Id, result.Id);
        }

        // Тест проверяет обновление существующего события
        [Fact]
        public async Task ChangeEvent_ShouldUpdateExistEvent()
        {
            //Arrange
            var created =  await _service.CreateEventAsync(new CreateEventDTO
            {
                Title = "Event1",
                StartAt = DateTime.Now,
                EndAt = DateTime.Now.AddHours(1),
                TotalSeats = 1
            });

            var createdEvent = created.ToEntity();
            createdEvent.Title = "New event";

            //Act
            _service.ChangeEvent(created.Id, createdEvent);

            var result = _service.GetEvent(createdEvent.Id);

            //Assert
            Assert.Equal(createdEvent.Title, result.Title);
        }

        // Тест проверяет удаление существующего события
        [Fact]
        public async Task RemoveEvent_ShouldRemoveEvent()
        {
            //Arrange
            var created = await _service.CreateEventAsync(new CreateEventDTO
            {
                Title = "Event1",
                StartAt = DateTime.Now,
                EndAt = DateTime.Now.AddHours(1),
                TotalSeats = 1
            });

            //Act
            _service.RemoveEvent(created.Id);

            //Assert
            Assert.Throws<NotFoundException>(() => _service.GetEvent(created.Id));
        }

        // Тест проверяет получение событий с фильтрацией по названию
        [Fact]
        public async Task GetEvents_ShouldFilterByTitle()
        {
            //Arrange
            var created = await _service.CreateEventAsync(new CreateEventDTO
            {
                Title = "Event1",
                StartAt = DateTime.Now,
                EndAt = DateTime.Now.AddHours(1),
                TotalSeats = 1 
            });

            var filter = new GetEventsRequestDTO { Title = "event" };

            //Act
            var result = _service.GetEvents(filter);

            //Assert
            Assert.Single(result.Items);
        }

        // Тест проверяет получение событий с фильтрацией по датам
        [Fact]
        public async Task GetEvents_ShouldFilterByDateRange()
        {
            //Arrange
            var created = await _service.CreateEventAsync(new CreateEventDTO
            {
                Title = "Event1",
                StartAt = new DateTime(2026, 7, 23),
                EndAt = new DateTime(2026, 7, 24),
                TotalSeats = 1
            });

            //Act
            var result = _service.GetEvents(new GetEventsRequestDTO
            {
                From = new DateTime(2026, 7, 1),
                To = new DateTime(2026, 7, 25)
            });
            //Assert
            Assert.Single(result.Items);
        }

        // Тест проверяет получение событий с пагинацией
        [Fact]
        public async Task GetEvents_ShouldReturnSecondPage()
        {
            //Arrange
            for (int i = 1; i < 16; i++)
            {
                var created = await _service.CreateEventAsync(new CreateEventDTO
                {
                    Title = $"Event{i}",
                    StartAt = new DateTime(2026, 7, 23),
                    EndAt = new DateTime(2026, 7, 24),
                    TotalSeats = 1
                });
            }

            //Act
            var result = _service.GetEvents(new GetEventsRequestDTO
            {
                Page = 2,
                PageSize = 10
            });

            //Assert
            Assert.Equal(5, result.Items.Count());
        }

        // Тест проверяет получение событий с комбинированной фильтрацией
        [Fact]
        public async Task GetEvents_ShouldApplyAllFilters()
        {
            //Arrange
            var created = await _service.CreateEventAsync(new CreateEventDTO
            {
                Title = "Event1",
                StartAt = new DateTime(2026, 7, 23),
                EndAt = new DateTime(2026, 7, 24),
                TotalSeats = 1
            });

            //Act
            var result = _service.GetEvents(new GetEventsRequestDTO
            {
                Title = "event",
                From = new DateTime(2026, 7, 1),
                To = new DateTime(2026, 7, 25)
            });

            //Assert
            Assert.Single(result.Items);
        }

        // Тест проверяет получение события с несуществующим ID
        [Fact]
        public void GetEvent_ShouldThrowNotFoundException()
        {
            Assert.Throws<NotFoundException>(() => _service.GetEvent(new Guid("14ce3d62-ee59-4399-ba23-41fa2a8a2935")));
        }

        // Тест проверяет обновление событие с несуществующим ID
        [Fact]
        public async Task ChangeEvent_ShouldThrowNotFoundException()
        {
            var newEvent = await _service.CreateEventAsync(new CreateEventDTO
            {
                Title = "Event new",
                StartAt = new DateTime(2026, 7, 23),
                EndAt = new DateTime(2026, 7, 24),
                TotalSeats = 1
            });

            Assert.Throws<NotFoundException>(() => _service.ChangeEvent(new Guid("14ce3d62-ee59-4399-ba23-41fa2a8a2935"),
                                                                        newEvent.ToEntity()));
        }

        // Тест проверяет создание события с некорректными данными
        [Fact]
        public async Task CreateEventAcync_ShouldThrowArgumentException_WhenTitleIsMissing()
        {
            //Arrange
            var newEvent = new CreateEventDTO
            {
                Title = string.Empty,
                StartAt = new DateTime(2026, 7, 23),
                EndAt = new DateTime(2026, 7, 24)
            };

            //Act && Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>  _service.CreateEventAsync(newEvent));
        }

        // Тест проверяет обновление события с некорректными датами
        [Fact]
        public async Task ChangeEvent_ShouldThrowArgumentException_WhenEndAtEarlierThenStartAt()
        {
            //Arrange
            var eventItem = await _service.CreateEventAsync(new CreateEventDTO
            {
                Title = "Event1",
                StartAt = new DateTime(2026, 7, 23),
                EndAt = new DateTime(2026, 7, 24),
                TotalSeats = 1
            });

            eventItem.EndAt = new DateTime(2026, 7, 22);

            //Act && Assert
            Assert.Throws<ArgumentException>(() => _service.ChangeEvent(eventItem.Id, eventItem.ToEntity()));
        }
    }
}
