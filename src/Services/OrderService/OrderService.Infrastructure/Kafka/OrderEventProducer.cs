using System.Text.Json;

namespace OrderService.Infrastructure.Kafka;

public interface IOrderEventProducer
{
    Task PublishAsync<TEvent>(string topic, TEvent @event, CancellationToken cancellationToken = default) where TEvent : class;
}

public class OrderEventProducer : IOrderEventProducer
{
    // For now, a simple implementation. In production, use Confluent.Kafka
    public async Task PublishAsync<TEvent>(string topic, TEvent @event, CancellationToken cancellationToken = default) where TEvent : class
    {
        // TODO: Implement actual Kafka producer
        // This is a placeholder for the actual implementation
        var eventJson = JsonSerializer.Serialize(@event);
        Console.WriteLine($"Publishing to {topic}: {eventJson}");
        await Task.CompletedTask;
    }
}
