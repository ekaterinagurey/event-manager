using Confluent.Kafka;
using EventManager.Events.Application.Interfaces;
using EventManager.Shared.Contracts.Events;
using EventManager.Shared.Contracts.Topics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace EventManager.Events.Infrastructure.Messaging
{
    public class BookingCancelledConsumer : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<BookingCancelledConsumer> _logger;

        public BookingCancelledConsumer(IServiceScopeFactory scopeFactory,
                                        IConfiguration configuration,
                                        ILogger<BookingCancelledConsumer> logger)
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
                ?? throw new InvalidOperationException("Kafka:BootstrapServers is missing.");

            var consumerGroup = _configuration["Kafka:ConsumerGroup"] ?? "events-service-group";

            var consumerConfig = new ConsumerConfig
            {
                BootstrapServers = bootstrapServers,
                GroupId = consumerGroup,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false
            };

            using var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
            consumer.Subscribe(KafkaTopics.BookingCancelled);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(stoppingToken);

                    var rawJson = result.Message.Value;
                    var currentEvent = JsonSerializer.Deserialize<BookingCancelledEvent>(rawJson);

                    if (currentEvent != null)
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();

                        var success = eventService.ReleaseSeatsAsync(currentEvent.EventId,
                                                                     currentEvent.SeatsCount,
                                                                     stoppingToken)
                            .GetAwaiter().GetResult();

                        if (success)
                        {
                            _logger.LogInformation($"Места успешно возвращены для события {currentEvent.EventId}");
                        }
                        else
                        {
                            _logger.LogWarning($"Не удалось вернуть места для события {currentEvent.EventId}");
                        }
                    }

                    consumer.Commit(result);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка обработки события отмены бронирования.");
                }
            }

            consumer.Close();
        }
    }
}
