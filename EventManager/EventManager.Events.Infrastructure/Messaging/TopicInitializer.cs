using Confluent.Kafka;
using Confluent.Kafka.Admin;
using EventManager.Shared.Contracts.Topics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EventManager.Events.Infrastructure.Messaging
{
    public class TopicInitializer : IHostedService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<TopicInitializer> _logger;

        public TopicInitializer(IConfiguration configuration, ILogger<TopicInitializer> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var bootstrapServers = _configuration["Kafka:BootstrapServers"];
            var adminConfig = new AdminClientConfig { BootstrapServers = bootstrapServers };

            try
            {
                using var adminClient = new AdminClientBuilder(adminConfig).Build();

                var metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(5));
                var topicExists = metadata.Topics.Any(t => t.Topic == KafkaTopics.BookingConfirmed);

                if (!topicExists)
                {
                    _logger.LogInformation($"Топик '{KafkaTopics.BookingConfirmed}' не найден.");

                    var topicSpecification = new TopicSpecification
                    {
                        Name = KafkaTopics.BookingConfirmed,
                        NumPartitions = 3,
                        ReplicationFactor = 1
                    };

                    await adminClient.CreateTopicsAsync(new[] { topicSpecification });
                    _logger.LogInformation($"Топик '{KafkaTopics.BookingConfirmed}' успешно создан.");
                }
            }
            catch (CreateTopicsException ex) 
            when (ex.Results.Any(r => r.Error.Code == ErrorCode.TopicAlreadyExists))
            {
                _logger.LogInformation($"Топик '{KafkaTopics.BookingConfirmed}' уже существует.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Не удалось создать топик при старте.");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
