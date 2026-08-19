using EventManager.DataAccess;
using EventManager.DTOs;
using EventManager.DTOs.Events;
using EventManager.Exceptions;
using EventManager.Mappers;
using EventManager.Models;
using EventManager.Repositories;
using EventManager.Repositories.Interfaces;
using EventManager.Services;
using EventManager.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EventManager.Tests
{
    public class EventServiceTests: IDisposable
    {
        private readonly ServiceProvider _serviceProvider;
        private readonly IServiceScope _scope;
        private readonly IEventService _eventService;

        public EventServiceTests()
        {
            var dbName = Guid.NewGuid().ToString();
            var services = new ServiceCollection();
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(dbName));
            services.AddScoped<IEventRepository, EventRepository>();
            services.AddScoped<IEventService, EventService>();

            _serviceProvider = services.BuildServiceProvider();
            _scope = _serviceProvider.CreateScope();
            _eventService = _scope.ServiceProvider.GetRequiredService<IEventService>();
        }

        public void Dispose()
        {
            _scope.Dispose();
            _serviceProvider.Dispose();
        }

        #region CreateEventAsync Tests

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
            var created = await _eventService.CreateEventAsync(newEvent);

            //Assert
            Assert.Equal(newEvent.Title, created.Title);
            Assert.Equal(newEvent.StartAt, created.StartAt);
            Assert.Equal(newEvent.EndAt, created.EndAt);
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
            await Assert.ThrowsAsync<ArgumentException>(() => _eventService.CreateEventAsync(newEvent));
        }

        #endregion

        #region GetEventsAsync Tests

        // Тест проверяет, что метод возвращает все события
        [Fact]
        public async Task GetEventsAsync_ShouldReturnAllEvents()
        {
            //Arrange
            await _eventService.CreateEventAsync(new CreateEventDTO
            {
                Title = "Event1",
                StartAt = DateTime.Now,
                EndAt = DateTime.Now.AddHours(1),
                TotalSeats = 1
            });

            //Act
            var events = await _eventService.GetEventsAsync(new GetEventsRequestDTO());

            //Assert
            Assert.Single(events.Items);
        }

        // Тест проверяет получение событий с фильтрацией по названию
        [Fact]
        public async Task GetEventsAsync_ShouldFilterByTitle()
        {
            //Arrange
            var created = await _eventService.CreateEventAsync(new CreateEventDTO
            {
                Title = "Event1",
                StartAt = DateTime.Now,
                EndAt = DateTime.Now.AddHours(1),
                TotalSeats = 1
            });

            var filter = new GetEventsRequestDTO { Title = "event" };

            //Act
            var result = await _eventService.GetEventsAsync(filter);

            //Assert
            Assert.Single(result.Items);
        }

        // Тест проверяет получение событий с фильтрацией по датам
        [Fact]
        public async Task GetEventsAsync_ShouldFilterByDateRange()
        {
            //Arrange
            var created = await _eventService.CreateEventAsync(new CreateEventDTO
            {
                Title = "Event1",
                StartAt = new DateTime(2026, 7, 23),
                EndAt = new DateTime(2026, 7, 24),
                TotalSeats = 1
            });

            //Act
            var result = await _eventService.GetEventsAsync(new GetEventsRequestDTO
            {
                From = new DateTime(2026, 7, 1),
                To = new DateTime(2026, 7, 25)
            });
            //Assert
            Assert.Single(result.Items);
        }

        // Тест проверяет получение событий с пагинацией
        [Fact]
        public async Task GetEventsAsync_ShouldReturnSecondPage()
        {
            //Arrange
            for (int i = 1; i < 16; i++)
            {
                var created = await _eventService.CreateEventAsync(new CreateEventDTO
                {
                    Title = $"Event{i}",
                    StartAt = new DateTime(2026, 7, 23),
                    EndAt = new DateTime(2026, 7, 24),
                    TotalSeats = 1
                });
            }

            //Act
            var result = await _eventService.GetEventsAsync(new GetEventsRequestDTO
            {
                Page = 2,
                PageSize = 10
            });

            //Assert
            Assert.Equal(5, result.Items.Count());
        }

        // Тест проверяет получение событий с комбинированной фильтрацией
        [Fact]
        public async Task GetEventsAsync_ShouldApplyAllFilters()
        {
            //Arrange
            var created = await _eventService.CreateEventAsync(new CreateEventDTO
            {
                Title = "Event1",
                StartAt = new DateTime(2026, 7, 23),
                EndAt = new DateTime(2026, 7, 24),
                TotalSeats = 1
            });

            //Act
            var result = await _eventService.GetEventsAsync(new GetEventsRequestDTO
            {
                Title = "event",
                From = new DateTime(2026, 7, 1),
                To = new DateTime(2026, 7, 25)
            });

            //Assert
            Assert.Single(result.Items);
        }
        #endregion

        #region GetEventByIdAsync Tests

        // Тест проверяет, что метод возвращает событие по Id
        [Fact]
        public async Task GetEventByIdAsync_ShouldReturnEvent()
        {
            //Arrange
            var created = await _eventService.CreateEventAsync(new CreateEventDTO
            {
                Title = "Event1",
                StartAt = DateTime.Now,
                EndAt = DateTime.Now.AddHours(1),
                TotalSeats = 1
            });

            //Act
            var result = await _eventService.GetEventByIdAsync(created.Id);

            //Assert
            Assert.Equal(created.Id, result.Id);
        }

        // Тест проверяет получение события с несуществующим ID
        [Fact]
        public async void GetEventByIdAsync_ShouldThrowNotFoundException()
        {
            await Assert.ThrowsAsync<NotFoundException>(() => _eventService.GetEventByIdAsync(new Guid("14ce3d62-ee59-4399-ba23-41fa2a8a2935")));
        }

        #endregion

        #region UpdateEventAsync Tests

        // Тест проверяет обновление существующего события
        [Fact]
        public async Task UpdateEventAsync_ShouldUpdateExistEvent()
        {
            //Arrange
            var created = await _eventService.CreateEventAsync(new CreateEventDTO
            {
                Title = "Event1",
                StartAt = DateTime.Now,
                EndAt = DateTime.Now.AddHours(1),
                TotalSeats = 1
            });

            var editingEvent = new UpdateEventDTO
            {
                Title = "Updated Event",
                StartAt = DateTime.Now,
                EndAt = DateTime.Now.AddHours(1)
            };

            //Act
            var result = await _eventService.UpdateEventAsync(created.Id, editingEvent);

            //Assert
            Assert.Equal(editingEvent.Title, result.Title);
        }

        // Тест проверяет обновление событие с несуществующим ID
        [Fact]
        public async Task UpdateEventAsync_ShouldThrowNotFoundException()
        {
            var newEvent = await _eventService.CreateEventAsync(new CreateEventDTO
            {
                Title = "Event new",
                StartAt = new DateTime(2026, 7, 23),
                EndAt = new DateTime(2026, 7, 24),
                TotalSeats = 1
            });

            var editingEvent = new UpdateEventDTO
            {
                Title = "Updated Event",
                StartAt = new DateTime(2026, 7, 23),
                EndAt = new DateTime(2026, 7, 24),
            };

            await Assert.ThrowsAsync<NotFoundException>(() => _eventService.UpdateEventAsync(new Guid("14ce3d62-ee59-4399-ba23-41fa2a8a2935"),
                                                                                              editingEvent));
        }

        // Тест проверяет обновление события с некорректными датами
        [Fact]
        public async Task UpdateEventAsync_ShouldThrowArgumentException_WhenEndAtEarlierThenStartAt()
        {
            //Arrange
            var newEvent = await _eventService.CreateEventAsync(new CreateEventDTO
            {
                Title = "Event1",
                StartAt = new DateTime(2026, 7, 23),
                EndAt = new DateTime(2026, 7, 24),
                TotalSeats = 1
            });

            newEvent.EndAt = new DateTime(2026, 7, 22);

            var editingEvent = new UpdateEventDTO
            {
                Title = "Updated Event",
                StartAt = new DateTime(2026, 7, 23),
                EndAt = new DateTime(2026, 7, 22)
            };

            //Act && Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _eventService.UpdateEventAsync(newEvent.Id, editingEvent));
        }

        #endregion

        #region RemoveEventAsync Tests

        // Тест проверяет удаление существующего события
        [Fact]
        public async Task RemoveEventAsync_ShouldRemoveEvent()
        {
            //Arrange
            var created = await _eventService.CreateEventAsync(new CreateEventDTO
            {
                Title = "Event1",
                StartAt = DateTime.Now,
                EndAt = DateTime.Now.AddHours(1),
                TotalSeats = 1
            });

            //Act
            await _eventService.RemoveEventAsync(created.Id);

            //Assert
            await Assert.ThrowsAsync<NotFoundException>(() => _eventService.GetEventByIdAsync(created.Id));
        }

        #endregion
    }
}
