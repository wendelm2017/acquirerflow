using AcquirerFlow.Settlement.Application.Services;
using AcquirerFlow.Settlement.Domain.Ports.Out;
using AcquirerFlow.Settlement.Worker;
using AcquirerFlow.Settlement.Worker.Consumers;
using AcquirerFlow.Settlement.Worker.Persistence;
using MassTransit;

var builder = Host.CreateDefaultBuilder(args);

builder.ConfigureServices(services =>
{
    // Domain + Application
    services.AddSingleton<ISettlementRepository, InMemorySettlementRepository>();
    services.AddSingleton<SettlementService>();

    // MassTransit + RabbitMQ
    services.AddMassTransit(x =>
    {
        x.AddConsumer<TransactionCapturedConsumer>();

        x.UsingRabbitMq((context, cfg) =>
        {
            cfg.Host("localhost", "/", h =>
            {
                h.Username("admin");
                h.Password("AcquirerFlow@2024");
            });

            cfg.ReceiveEndpoint("settlement-capture-queue", e =>
            {
                e.ConfigureConsumer<TransactionCapturedConsumer>(context);
                e.UseMessageRetry(r => r.Intervals(1000, 5000, 30000)); // Retry: 1s, 5s, 30s
            });
        });
    });

    // Batch worker
    services.AddHostedService<SettlementBatchWorker>();
});

var host = builder.Build();
host.Run();
