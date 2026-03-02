using System.Text.Json;
using AcquirerFlow.Capture.Application.Services;
using AcquirerFlow.Contracts.Events;
using Confluent.Kafka;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AcquirerFlow.Capture.Worker;

public class KafkaCaptureWorker : BackgroundService
{
    private readonly ILogger<KafkaCaptureWorker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IBus _bus;

    public KafkaCaptureWorker(ILogger<KafkaCaptureWorker> logger, IServiceScopeFactory scopeFactory, IBus bus)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _bus = bus;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[CAPTURE WORKER] Listening on Kafka topic: transaction-authorized");

        var config = new ConsumerConfig
        {
            BootstrapServers = "localhost:9092",
            GroupId = "capture-service",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        using var consumer = new ConsumerBuilder<Ignore, string>(config).Build();
        consumer.Subscribe("transaction-authorized");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = consumer.Consume(TimeSpan.FromSeconds(1));
                if (result is null) continue;

                _logger.LogInformation("[CAPTURE WORKER] Kafka msg | Partition: [{p}] | Offset: {o}",
                    result.Partition.Value, result.Offset.Value);

                var evt = JsonSerializer.Deserialize<TransactionAuthorized>(result.Message.Value);
                if (evt is null) continue;

                Console.WriteLine($"[KAFKA] Topic: transaction-authorized | Partition: [{result.Partition.Value}] | Offset: {result.Offset.Value}");

                using var scope = _scopeFactory.CreateScope();
                var captureService = scope.ServiceProvider.GetRequiredService<CaptureService>();

                await captureService.ProcessAuthorizationAsync(evt);

                Console.WriteLine($"[CAPTURE] Transaction {evt.TransactionId} captured: {evt.Currency} {evt.Amount:F2}");

                // Publish to RabbitMQ
                var capturedEvent = new TransactionCaptured(
                    evt.TransactionId, evt.MerchantId, evt.CardToken, evt.CardBrand,
                    evt.Amount, evt.Currency, evt.Installments, evt.Type,
                    evt.AuthorizationCode, DateTime.UtcNow);

                await _bus.Publish(capturedEvent, stoppingToken);
                _logger.LogInformation("[CAPTURE WORKER] Published TransactionCaptured to RabbitMQ");

                consumer.Commit(result);
            }
            catch (ConsumeException ex)
            {
                _logger.LogError(ex, "[CAPTURE WORKER] Kafka consume error");
            }
        }
    }
}
