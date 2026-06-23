using EventManager.DTOs;
using EventManager.Exceptions;
using EventManager.Models;
using EventManager.Services;
using EventManager.Tests.Helpers;

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
        public void AddEvent_SchouldCreateEvents()
        {
            //Arrange
            var newEvent = new Event
            {
                Id = 1,
                Title = "New Event",
                StartAt = new DateTime(2026, 7, 23),
                EndAt = new DateTime(2026, 7, 24)
            };

            //Act
            var created = _service.AddEvent(newEvent);

            //Assert
            Assert.Equal(newEvent.Id, created.Id);
            Assert.Equal(newEvent.Title, created.Title);
            Assert.Equal(newEvent.StartAt, created.StartAt);
            Assert.Equal(newEvent.EndAt, created.EndAt);
        }

        // Тест проверяет, что метод возвращает все события
        [Fact]
        public void GetEvents_SchouldReturnAllEvents()
        {
            //Arrange
            _service.AddEvent(new Event
            {
                Id = 1,
                Title = "Event1",
                StartAt = DateTime.Now,
                EndAt = DateTime.Now.AddHours(1)
            });

            //Act
            var events = _service.GetEvents(new GetEventsRequestDTO());

            //Assert
            Assert.Single(events.Items);
        }

        // Тест проверяет, что метод возвращает событие по Id
        [Fact]
        public void GetEvent_SchouldReturnEvent()
        {
            //Arrange
            var created = _service.AddEvent(new Event
            {
                Id = 1,
                Title = "Event1",
                StartAt = DateTime.Now,
                EndAt = DateTime.Now.AddHours(1)
            });

            //Act
            var result = _service.GetEvent(created.Id);

            //Assert
            Assert.Equal(created.Id, result.Id);
        }

        // Тест проверяет обновление существующего события
        [Fact]
        public void ChangeEvent_SchouldUpdateExistEvent()
        {
            //Arrange
            var created = _service.AddEvent(new Event
            {
                Id = 1,
                Title = "Event1",
                StartAt = DateTime.Now,
                EndAt = DateTime.Now.AddHours(1)
            });

            created.Title = "New event";

            //Act
            _service.ChangeEvent(created.Id, created);

            var result = _service.GetEvent(created.Id);

            //Assert
            Assert.Equal(created.Title, result.Title);
        }

        // Тест проверяет удаление существующего события
        [Fact]
        public void RemoveEvent_SchouldRemoveEvent()
        {
            //Arrange
            var created = _service.AddEvent(new Event
            {
                Id = 1,
                Title = "Event1",
                StartAt = DateTime.Now,
                EndAt = DateTime.Now.AddHours(1)
            });

            //Act
            _service.RemoveEvent(created.Id);

            //Assert
            Assert.Throws<NotFoundException>(() => _service.GetEvent(created.Id));
        }

        // Тест проверяет получение событий с фильтрацией по названию
        [Fact]
        public void GetEvents_SchouldFilterByTitle()
        {
            //Arrange
            var created = _service.AddEvent(new Event
            {
                Id = 1,
                Title = "Event1",
                StartAt = DateTime.Now,
                EndAt = DateTime.Now.AddHours(1)
            });

            var filter = new GetEventsRequestDTO { Title = "event" };

            //Act
            var result = _service.GetEvents(filter);

            //Assert
            Assert.Single(result.Items);
        }

        // Тест проверяет получение событий с фильтрацией по датам
        [Fact]
        public void GetEvents_SchouldFilterByDateRange()
        {
            //Arrange
            var created = _service.AddEvent(new Event
            {
                Id = 1,
                Title = "Event1",
                StartAt = new DateTime(2026, 7, 23),
                EndAt = new DateTime(2026, 7, 24)
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
        public void GetEvents_SchouldReturnSecondPage()
        {
            //Arrange
            for (int i = 1; i < 16; i++)
            {
                var created = _service.AddEvent(new Event
                {
                    Id = i,
                    Title = $"Event{i}",
                    StartAt = new DateTime(2026, 7, 23),
                    EndAt = new DateTime(2026, 7, 24)
                });
            }

            //Act
            var result = _service.GetEvents(new GetEventsRequestDTO
            {
                PageNumber = 2,
                PageSize = 10
            });

            //Assert
            Assert.Equal(5, result.Items.Count());
        }

        // Тест проверяет получение событий с комбинированной фильтрацией
        [Fact]
        public void GetEvents_SchouldApplyAllFilters()
        {
            //Arrange
            var created = _service.AddEvent(new Event
            {
                Id = 1,
                Title = "Event1",
                StartAt = new DateTime(2026, 7, 23),
                EndAt = new DateTime(2026, 7, 24)
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
        public void GetEvent_SchouldThrowNotFoundException()
        {
            Assert.Throws<NotFoundException>(() => _service.GetEvent(999));
        }

        // Тест проверяет обновление событие с несуществующим ID
        [Fact]
        public void ChangeEvent_SchouldThrowNotFoundException()
        {
            var newEvent = _service.AddEvent(new Event
            {
                Id = 1,
                Title = "Event new",
                StartAt = new DateTime(2026, 7, 23),
                EndAt = new DateTime(2026, 7, 24)
            });

            Assert.Throws<NotFoundException>(() => _service.ChangeEvent(999, newEvent));
        }

        // Тест проверяет создание события с некорректными данными
        [Fact]
        public void AddEvent_SchouldThrowNotValidateException_WhenEndAtEarlierThenStartAt()
        {
            var newEvent = new EventDTO
            {
                Id = 1,
                Title = "Event new",
                StartAt = new DateTime(2026, 7, 2),
                EndAt = new DateTime(2026, 7, 1)
            };

            var results = ValidationHelper.ValidateModel(newEvent);
            Assert.NotEmpty(results);
            Assert.Contains(results, r => r.MemberNames.Contains(nameof(newEvent.EndAt)));
        }
    }
}
