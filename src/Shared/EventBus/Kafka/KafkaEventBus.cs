using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using EventBus.Abstractions;
using Microsoft.Extensions.Configuration;

namespace EventBus.Kafka;

public class KafkaEventBus : IEventBus
{
    private readonly IConfiguration _configuration;
    private readonly IProducer<string, string> _producer;

    public KafkaEventBus(IConfiguration configuration)
    {
        _configuration = configuration;
        var config = new ProducerConfig
        {
            BootstrapServers = _configuration["Kafka:BootstrapServers"] ?? "localhost:9092"
        };
        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task PublishAsync<T>(T @event) where T : IntegrationEvent
    {
        var topic = typeof(T).Name;
        var message = JsonSerializer.Serialize(@event);
        
        await _producer.ProduceAsync(topic, new Message<string, string>
        {
            Key = @event.Id.ToString(),
            Value = message
        });
    }

    public void Subscribe<T, TH>()
        where T : IntegrationEvent
        where TH : IIntegrationEventHandler<T>
    {
        throw new NotImplementedException();
    }
}
