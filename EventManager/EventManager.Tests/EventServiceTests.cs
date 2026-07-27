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
        public void CreateEventAcync_ShouldCreateEvents()
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
            var created = _service.CreateEventAcync(newEvent).Result;

            //Assert
            Assert.Equal(newEvent.Title, created.Title);
            Assert.Equal(newEvent.StartAt, created.StartAt);
            Assert.Equal(newEvent.EndAt, created.EndAt);
        }

        // Тест проверяет, что метод возвращает все события
        [Fact]
        public void GetEvents_ShouldReturnAllEvents()
        {
            //Arrange
            _service.CreateEventAcync(new CreateEventDTO
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
        public void GetEvent_ShouldReturnEvent()
        {
            //Arrange
            var created = _service.CreateEventAcync(new CreateEventDTO
            {
                Title = "Event1",
                StartAt = DateTime.Now,
                EndAt = DateTime.Now.AddHours(1),
                TotalSeats = 1
            }).Result;

            //Act
            var result = _service.GetEvent(created.Id);

            //Assert
            Assert.Equal(created.Id, result.Id);
        }

        // Тест проверяет обновление существующего события
        [Fact]
        public void ChangeEvent_ShouldUpdateExistEvent()
        {
            //Arrange
            var created =  _service.CreateEventAcync(new CreateEventDTO
            {
                Title = "Event1",
                StartAt = DateTime.Now,
                EndAt = DateTime.Now.AddHours(1),
                TotalSeats = 1
            }).Result.ToEntity();

            created.Title = "New event";

            //Act
            _service.ChangeEvent(created.Id, created);

            var result = _service.GetEvent(created.Id);

            //Assert
            Assert.Equal(created.Title, result.Title);
        }

        // Тест проверяет удаление существующего события
        [Fact]
        public void RemoveEvent_ShouldRemoveEvent()
        {
            //Arrange
            var created = _service.CreateEventAcync(new CreateEventDTO
            {
                Title = "Event1",
                StartAt = DateTime.Now,
                EndAt = DateTime.Now.AddHours(1),
                TotalSeats = 1
            }).Result;

            //Act
            _service.RemoveEvent(created.Id);

            //Assert
            Assert.Throws<NotFoundException>(() => _service.GetEvent(created.Id));
        }

        // Тест проверяет получение событий с фильтрацией по названию
        [Fact]
        public void GetEvents_ShouldFilterByTitle()
        {
            //Arrange
            var created = _service.CreateEventAcync(new CreateEventDTO
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
        public void GetEvents_ShouldFilterByDateRange()
        {
            //Arrange
            var created = _service.CreateEventAcync(new CreateEventDTO
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
        public void GetEvents_ShouldReturnSecondPage()
        {
            //Arrange
            for (int i = 1; i < 16; i++)
            {
                var created = _service.CreateEventAcync(new CreateEventDTO
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
        public void GetEvents_ShouldApplyAllFilters()
        {
            //Arrange
            var created = _service.CreateEventAcync(new CreateEventDTO
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
        public void ChangeEvent_ShouldThrowNotFoundException()
        {
            var newEvent = _service.CreateEventAcync(new CreateEventDTO
            {
                Title = "Event new",
                StartAt = new DateTime(2026, 7, 23),
                EndAt = new DateTime(2026, 7, 24),
                TotalSeats = 1
            }).Result;

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
            await Assert.ThrowsAsync<ArgumentException>(() =>  _service.CreateEventAcync(newEvent));
        }

        // Тест проверяет обновление события с некорректными датами
        [Fact]
        public void ChangeEvent_ShouldThrowArgumentException_WhenEndAtEarlierThenStartAt()
        {
            //Arrange
            var eventItem = _service.CreateEventAcync(new CreateEventDTO
            {
                Title = "Event1",
                StartAt = new DateTime(2026, 7, 23),
                EndAt = new DateTime(2026, 7, 24),
                TotalSeats = 1
            }).Result;

            eventItem.EndAt = new DateTime(2026, 7, 22);

            //Act && Assert
            Assert.Throws<ArgumentException>(() => _service.ChangeEvent(eventItem.Id, eventItem.ToEntity()));
        }
    }
}
