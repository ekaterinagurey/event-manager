using Confluent.Kafka;
using EventManager.Events.Application.Interfaces;
using EventManager.Shared.Contracts.Events;
using EventManager.Shared.Contracts.Topics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace EventManager.Events.Infrastructure.Messaging
{
    public class BookingConfirmedConsumer : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<BookingConfirmedConsumer> _logger;

        public BookingConfirmedConsumer(IServiceScopeFactory scopeFactory,
                                        IConfiguration configuration,
                                        ILogger<BookingConfirmedConsumer> logger)
        {
            _scopeFactory = scopeFactory;
            _configuration = configuration;
            _logger = logger;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.Run(() => StartConsumerLoop(stoppingToken), stoppingToken);
        }

        private void StartConsumerLoop(CancellationToken stoppingToken)
        {
            var bootstrapServers = _configuration["Kafka:BootstrapServers"]
                ?? throw new InvalidOperationException("Kafka:BootstrapServers configuration is missing.");

            var consumerGroup = _configuration["Kafka:ConsumerGroup"] ?? "events-service-group";

            var consumerConfig = new ConsumerConfig
            {
                BootstrapServers = bootstrapServers,
                GroupId = consumerGroup,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false
            };

            using var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();

            consumer.Subscribe(KafkaTopics.BookingConfirmed);
            _logger.LogInformation($"Consumer подписан на топик '{KafkaTopics.BookingConfirmed}'. Группа: {consumerGroup}");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = consumer.Consume(stoppingToken);
                    ProcessMessage(consumer, consumeResult, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Непредвиденная ошибка при обработке сообщения.");
                }
            }

            consumer.Close();
        }

        private void ProcessMessage(IConsumer<string, string> consumer,
                                    ConsumeResult<string, string> consumeResult,
                                    CancellationToken stoppingToken)
        {
            try
            {
                var rawJson = consumeResult.Message.Value;
                var currentEvent = JsonSerializer.Deserialize<BookingConfirmedEvent>(rawJson);

                if (currentEvent is null)
                {
                    _logger.LogWarning($"Cобытие не найдено: {rawJson}");
                    consumer.Commit(consumeResult);
                    return;
                }

                using (var scope = _scopeFactory.CreateScope())
                {
                    var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();

                    var success = eventService.DecreaseAvailableSeatsAsync(
                        currentEvent.EventId,
                        currentEvent.SeatsCount,
                        stoppingToken).GetAwaiter().GetResult();

                    if (!success)
                    {
                        _logger.LogWarning($"Не удалось забронировать места для события {currentEvent.EventId} (Бронирование: {currentEvent.BookingId}).");
                    }
                    else
                    {
                        _logger.LogInformation($"Успешно забронировано мест: {currentEvent.SeatsCount} для события {currentEvent.EventId} (Бронирвание {currentEvent.BookingId}).");
                    }
                }

                consumer.Commit(consumeResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка обработки события.");
                throw;
            }
        }
    }
}
