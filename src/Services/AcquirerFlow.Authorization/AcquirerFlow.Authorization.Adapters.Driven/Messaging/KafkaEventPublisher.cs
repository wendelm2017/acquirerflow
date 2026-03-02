using System.Text.Json;
using Confluent.Kafka;
using AcquirerFlow.Authorization.Domain.Ports.Out;

namespace AcquirerFlow.Authorization.Adapters.Driven.Messaging;

public class KafkaEventPublisher : IEventPublisher, IDisposable
{
    private readonly IProducer<string, string> _producer;

    public KafkaEventPublisher(string bootstrapServers = "localhost:9092")
    {
        var config = new ProducerConfig
        {
            BootstrapServers = bootstrapServers,
            Acks = Acks.All,
            EnableIdempotence = true,
            MessageSendMaxRetries = 3
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task PublishAsync<T>(T @event, string topic) where T : class
    {
        var json = JsonSerializer.Serialize(@event);
        var key = Guid.NewGuid().ToString();

        var result = await _producer.ProduceAsync(topic, new Message<string, string>
        {
            Key = key,
            Value = json
        });

        Console.WriteLine($"[KAFKA] Topic: {topic} | Partition: {result.Partition} | Offset: {result.Offset}");
    }

    public void Dispose() => _producer?.Dispose();
}
