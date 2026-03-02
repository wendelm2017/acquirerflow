using System.Text.Json;
using Confluent.Kafka;
using AcquirerFlow.Capture.Application.Services;
using AcquirerFlow.Contracts.Events;
using MassTransit;

namespace AcquirerFlow.Capture.Worker;

public class KafkaCaptureWorker : BackgroundService
{
    private readonly CaptureService _captureService;
    private readonly IBus _bus;
    private readonly IConsumer<string, string> _consumer;
    private readonly ILogger<KafkaCaptureWorker> _logger;
    private const string Topic = "transaction-authorized";

    public KafkaCaptureWorker(CaptureService captureService, IBus bus, ILogger<KafkaCaptureWorker> logger)
    {
        _captureService = captureService;
        _bus = bus;
        _logger = logger;

        var config = new ConsumerConfig
        {
            BootstrapServers = "localhost:9092",
            GroupId = "capture-service",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        _consumer = new ConsumerBuilder<string, string>(config).Build();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _consumer.Subscribe(Topic);
        _logger.LogInformation("[CAPTURE WORKER] Listening on Kafka topic: {Topic}", Topic);

        await Task.Yield();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = _consumer.Consume(TimeSpan.FromSeconds(1));
                if (result == null) continue;

                _logger.LogInformation("[CAPTURE WORKER] Kafka msg | Partition: {P} | Offset: {O}",
                    result.Partition, result.Offset);

                var @event = JsonSerializer.Deserialize<TransactionAuthorized>(result.Message.Value);

                if (@event != null)
                {
                    await _captureService.ProcessAuthorizationAsync(@event);
                    _consumer.Commit(result);

                    // Publica TransactionCaptured no RabbitMQ pro Settlement
                    var capturedEvent = new TransactionCaptured(
                        @event.TransactionId,
                        @event.MerchantId,
                        @event.CardToken,
                        @event.CardBrand,
                        @event.Amount,
                        @event.Currency,
                        @event.Installments,
                        @event.Type,
                        @event.AuthorizationCode,
                        DateTime.UtcNow);

                    await _bus.Publish(capturedEvent, stoppingToken);
                    _logger.LogInformation("[CAPTURE WORKER] Published TransactionCaptured to RabbitMQ");
                }
            }
            catch (ConsumeException ex)
            {
                _logger.LogError(ex, "[CAPTURE WORKER] Kafka consume error");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CAPTURE WORKER] Processing error");
            }
        }

        _consumer.Close();
    }

    public override void Dispose()
    {
        _consumer?.Dispose();
        base.Dispose();
    }
}
