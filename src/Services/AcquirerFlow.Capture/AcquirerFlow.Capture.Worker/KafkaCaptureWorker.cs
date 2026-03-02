using System.Text.Json;
using Confluent.Kafka;
using AcquirerFlow.Capture.Application.Services;
using AcquirerFlow.Contracts.Events;

namespace AcquirerFlow.Capture.Worker;

public class KafkaCaptureWorker : BackgroundService
{
    private readonly CaptureService _captureService;
    private readonly IConsumer<string, string> _consumer;
    private readonly ILogger<KafkaCaptureWorker> _logger;
    private const string Topic = "transaction-authorized";

    public KafkaCaptureWorker(CaptureService captureService, ILogger<KafkaCaptureWorker> logger)
    {
        _captureService = captureService;
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
        _logger.LogInformation("[CAPTURE WORKER] Listening on topic: {Topic}", Topic);

        await Task.Yield(); // libera a thread pra não bloquear startup

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = _consumer.Consume(TimeSpan.FromSeconds(1));

                if (result == null) continue;

                _logger.LogInformation("[CAPTURE WORKER] Received message | Partition: {Partition} | Offset: {Offset}",
                    result.Partition, result.Offset);

                var @event = JsonSerializer.Deserialize<TransactionAuthorized>(result.Message.Value);

                if (@event != null)
                {
                    await _captureService.ProcessAuthorizationAsync(@event);
                    _consumer.Commit(result);
                    _logger.LogInformation("[CAPTURE WORKER] Committed offset {Offset}", result.Offset);
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
