using System.Text.Json;
using AcquirerFlow.Authorization.Domain.Ports.Out;

namespace AcquirerFlow.Authorization.Adapters.Driven.Messaging;

public class ConsoleEventPublisher : IEventPublisher
{
    public Task PublishAsync<T>(T @event, string topic) where T : class
    {
        var json = JsonSerializer.Serialize(@event, new JsonSerializerOptions { WriteIndented = true });
        Console.WriteLine($"[EVENT] Topic: {topic}");
        Console.WriteLine(json);
        Console.WriteLine();
        return Task.CompletedTask;
    }
}
