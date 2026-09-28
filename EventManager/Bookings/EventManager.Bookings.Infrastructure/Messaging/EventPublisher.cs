using Confluent.Kafka;
using EventManager.Bookings.Application.Interfaces;
using EventManager.Shared.Contracts.Events;
using EventManager.Shared.Contracts.Topics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace EventManager.Bookings.Infrastructure.Messaging
{
    public class EventPublisher : IEventPublisher, IDisposable
    {
        private readonly IProducer<string, string> _producer;
        private readonly ILogger<EventPublisher> _logger;

        public EventPublisher(IConfiguration configuration, ILogger<EventPublisher> logger)
        {
            _logger = logger;

            var bootstrapServers = configuration["Kafka:BootstrapServers"]
                ?? throw new InvalidOperationException("Kafka:BootstrapServers configuration is missing.");

            var producerConfig = new ProducerConfig
            {
                BootstrapServers = bootstrapServers,
                Acks = Acks.All
            };

            _producer = new ProducerBuilder<string, string>(producerConfig).Build();
        }

        public async Task PublishBookingConfirmedAsync(BookingConfirmedEvent currentEvent, 
                                                       CancellationToken cancellationToken = default)
        {
            var key = currentEvent.EventId.ToString();
            var payload = JsonSerializer.Serialize(currentEvent);

            var message = new Message<string, string>
            {
                Key = key,
                Value = payload
            };

            try
            {
                var deliveryResult = await _producer.ProduceAsync(KafkaTopics.BookingConfirmed,
                                                                  message,
                                                                  cancellationToken);

                _logger.LogInformation($"Kafka event published: Topic={deliveryResult.Topic},Key={key}");
            }
            catch (ProduceException<string, string> ex)
            {
                _logger.LogError(ex, $"Failed to deliver message to Kafka topic {KafkaTopics.BookingConfirmed} for BookingId={currentEvent.BookingId}. Error: {ex.Error.Reason}");
                throw;
            }
        }

        public async Task PublishBookingCancelledAsync(BookingCancelledEvent currentEvent,
                                                       CancellationToken cancellationToken = default)
        {
            var key = currentEvent.EventId.ToString();
            var payload = JsonSerializer.Serialize(currentEvent);

            await _producer.ProduceAsync(KafkaTopics.BookingCancelled,
                                         new Message<string, string> { Key = key, Value = payload },
                                         cancellationToken);

            _logger.LogInformation($"Опубликовано событие BookingCancelled для BookingId={currentEvent.BookingId}");
        }

        public void Dispose()
        {
            try
            {
                _producer.Flush(TimeSpan.FromSeconds(10));
                _producer.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error while disposing Kafka producer.");
            }
        }
    }
}
