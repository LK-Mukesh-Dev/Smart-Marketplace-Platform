using System.Text.Json;

namespace InventoryService.Infrastructure.Kafka;

public interface IInventoryEventProducer
{
    Task PublishAsync<TEvent>(string topic, TEvent @event, CancellationToken cancellationToken = default) where TEvent : class;
}

public class InventoryEventProducer : IInventoryEventProducer
{
    // Placeholder for actual Kafka producer implementation
    // In production, use Confluent.Kafka
    public async Task PublishAsync<TEvent>(string topic, TEvent @event, CancellationToken cancellationToken = default) where TEvent : class
    {
        var eventJson = JsonSerializer.Serialize(@event);
        Console.WriteLine($"Publishing to {topic}: {eventJson}");
        await Task.CompletedTask;
    }
}
