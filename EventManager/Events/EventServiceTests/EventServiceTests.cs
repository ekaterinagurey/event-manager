using EventManager.Events.Application.DTOs;
using EventManager.Events.Application.Interfaces;
using EventManager.Events.Application.Options;
using EventManager.Events.Application.Services;
using EventManager.Events.Domain.Entities;
using EventManager.Events.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace EventServiceTests
{
    public class EventServiceTests
    {
        private readonly Mock<IEventRepository> _eventRepositoryMock;
        private readonly Mock<ICacheService> _cacheServiceMock;
        private readonly Mock<ILogger<EventService>> _loggerMock;
        private readonly IOptions<RedisCacheOptions> _cacheOptions;
        private readonly EventService _eventService;

        public EventServiceTests()
        {
            _eventRepositoryMock = new Mock<IEventRepository>();
            _cacheServiceMock = new Mock<ICacheService>();
            _loggerMock = new Mock<ILogger<EventService>>();

            _cacheOptions = Options.Create(new RedisCacheOptions
            {
                ConnectionString = "localhost:6379",
                EventByIdTtlMinutes = 600,
                TopEventsTtlMinutes = 120
            });

            _eventService = new EventService(
                _eventRepositoryMock.Object,
                _cacheServiceMock.Object,
                _cacheOptions,
                _loggerMock.Object);
        }

        #region Сценарии попадания в кэш

        //Тест проверяет, что при получении события по Id при попадании в кеш репозиторий не вызывается
        [Fact]
        public async Task GetEventByIdAsync_WhenCacheHit_ReturnsCachedDataAndDoesNotCallRepository()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var cacheKey = $"event:{eventId}";

            var cachedDto = new EventInfoDTO()
            {
                Id = eventId,
                Title = "Tech Conference",
                Description = "Description",
                TotalSeats = 100,
                AvailableSeats = 20,
                StartAt = DateTime.UtcNow,
                EndAt = DateTime.UtcNow.AddDays(1)
            };

            _cacheServiceMock
                .Setup(c => c.GetAsync<EventInfoDTO>(cacheKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(cachedDto);

            // Act
            var result = await _eventService.GetEventByIdAsync(eventId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(result, cachedDto);

            // Ключевая проверка: репозиторий НЕ должен вызываться
            _eventRepositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(),
                                            It.IsAny<CancellationToken>()),
                                            Times.Never);
        }

        //Тест проверяет, что при получении топ 10 событий при попадании в кеш репозиторий не вызывается
        [Fact]
        public async Task GetTopPopularEventsAsync_WhenCacheHit_ReturnsCachedListAndDoesNotCallRepository()
        {
            // Arrange
            const string cacheKey = "events:top10";

            var eventId = Guid.NewGuid();
            var cachedDto = new EventInfoDTO()
            {
                Id = eventId,
                Title = "Tech Conference",
                Description = "Description",
                TotalSeats = 100,
                AvailableSeats = 20,
                StartAt = DateTime.UtcNow,
                EndAt = DateTime.UtcNow.AddDays(1)
            };

            var cachedList = new List<EventInfoDTO> { cachedDto };

            _cacheServiceMock
                .Setup(c => c.GetAsync<List<EventInfoDTO>>(cacheKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(cachedList);

            // Act
            var result = await _eventService.GetTopPopularEventsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(result, cachedList);

            _eventRepositoryMock.Verify(r => r.GetTopEventsAsync(It.IsAny<int>(),
                                             It.IsAny<CancellationToken>()),
                                             Times.Never);
        }
        #endregion

        #region Сценарии промаха кэша

        [Fact]
        public async Task GetEventByIdAsync_WhenCacheMiss_FetchesFromRepositoryAndSavesToCache()
        {
            // Arrange
            var expectedTtl = TimeSpan.FromSeconds(_cacheOptions.Value.EventByIdTtlMinutes);

            var expectedEvent = Event.Create("Test Event",
                                             DateTime.UtcNow,
                                             DateTime.UtcNow.AddDays(3),
                                             50,
                                             "Test");

            var entity = new EventInfoDTO
            {
                Id = expectedEvent.Id,
                Title = expectedEvent.Title,
                TotalSeats = expectedEvent.TotalSeats,
                AvailableSeats = expectedEvent.AvailableSeats,
                StartAt = expectedEvent.StartAt,
                EndAt = expectedEvent.EndAt,
                Description = expectedEvent.Description
            };

            var cacheKey = $"event:{expectedEvent.Id}";

            // Промах кэша
            _cacheServiceMock
                .Setup(c => c.GetAsync<EventInfoDTO>(cacheKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync((EventInfoDTO?)null);

            _eventRepositoryMock
                .Setup(r => r.GetByIdAsync(expectedEvent.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedEvent);

            // Act
            var result = await _eventService.GetEventByIdAsync(expectedEvent.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(entity.Id, result.Id);
            Assert.Equal(entity.Title, result.Title);
            Assert.Equal(entity.TotalSeats, result.TotalSeats);
            Assert.Equal(entity.AvailableSeats, result.AvailableSeats);
            Assert.Equal(entity.Description, result.Description);

            _eventRepositoryMock.Verify(r => r.GetByIdAsync(expectedEvent.Id, It.IsAny<CancellationToken>()),
                                             Times.Once);

            _cacheServiceMock.Verify(c => c.SetAsync(cacheKey,
                                                      It.Is<EventInfoDTO>(dto => dto.Id == expectedEvent.Id),
                                                      expectedTtl,
                                                      It.IsAny<CancellationToken>()),
                                            Times.Once);
        }

        [Fact]
        public async Task GetTopPopularEventsAsync_WhenCacheMiss_FetchesFromRepositoryAndSavesToCache()
        {
            // Arrange
            const string cacheKey = "events:top10";
            var expectedTtl = TimeSpan.FromSeconds(_cacheOptions.Value.TopEventsTtlMinutes);

            var expectedEvent = Event.Create(
                "Test Event",
                DateTime.UtcNow,
                DateTime.UtcNow.AddDays(3),
                50,
                "Test");

            var repoEvents = new List<Event> { expectedEvent };

            // Промах кэша
            _cacheServiceMock
                .Setup(c => c.GetAsync<List<EventInfoDTO>>(cacheKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync((List<EventInfoDTO>?)null);

            _eventRepositoryMock
                .Setup(r => r.GetTopEventsAsync(10, It.IsAny<CancellationToken>()))
                .ReturnsAsync(repoEvents);

            // Act
            var result = await _eventService.GetTopPopularEventsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(expectedEvent.Id, result.First().Id);

            // Проверяем вызов репозитория
            _eventRepositoryMock.Verify(
                r => r.GetTopEventsAsync(10, It.IsAny<CancellationToken>()),
                Times.Once);

            _cacheServiceMock.Verify(
                c => c.SetAsync(
                    cacheKey,
                    It.Is<IEnumerable<EventInfoDTO>>(items => items.Any(dto => dto.Id == expectedEvent.Id)),
                    expectedTtl,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        #endregion

        #region Мутирующие операции и инвалидация кэша

        [Fact]
        public async Task UpdateEventAsync_WhenEventUpdated_InvalidatesCacheKey()
        {
            // Arrange
            var existingEntity = Event.Create(
                "Old Title",
                DateTime.UtcNow,
                DateTime.UtcNow.AddDays(3),
                50,
                "Test");

            var cacheKey = $"event:{existingEntity.Id}";

            var updateDto = new UpdateEventDTO
            {
                Title = "New Title",
                Description = "New Desc",
                StartAt = existingEntity.StartAt,
                EndAt = existingEntity.EndAt
            };

            _eventRepositoryMock
                .Setup(r => r.GetByIdAsync(existingEntity.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingEntity);

            // Act
            await _eventService.UpdateEventAsync(existingEntity.Id, updateDto);

            // Assert
            _cacheServiceMock.Verify(c => c.RemoveAsync(cacheKey, It.IsAny<CancellationToken>()),
                                     Times.Once);
        }

        [Fact]
        public async Task DeleteEventAsync_WhenEventDeleted_InvalidatesCacheKey()
        {
            // Arrange
            var existingEntity = Event.Create(
                "To Delete",
                DateTime.UtcNow,
                DateTime.UtcNow.AddDays(3),
                50,
                "Test");

            var cacheKey = $"event:{existingEntity.Id}";

            var repoEvents = new List<Event> { existingEntity };

            _eventRepositoryMock
                .Setup(r => r.GetByIdAsync(existingEntity.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingEntity);

            // Act
            await _eventService.RemoveEventAsync(existingEntity.Id);

            // Assert
            _cacheServiceMock.Verify(c => c.RemoveAsync(cacheKey, It.IsAny<CancellationToken>()),
                                     Times.Once);
        }

        #endregion
    }
}
