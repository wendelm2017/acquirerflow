using AcquirerFlow.Settlement.Application.Services;
using AcquirerFlow.Settlement.Domain.Ports.Out;
using AcquirerFlow.Settlement.Worker;
using AcquirerFlow.Settlement.Worker.Consumers;
using AcquirerFlow.Infrastructure;
using AcquirerFlow.Infrastructure.Repositories;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;

var builder = Host.CreateDefaultBuilder(args);

builder.ConfigureServices(services =>
{
    var connectionString = "Server=localhost,1433;Database=AcquirerFlowDb;User Id=sa;Password=AcquirerFlow@2024;TrustServerCertificate=True";
    services.AddAcquirerFlowInfrastructure(connectionString);

    // SettlementService é Singleton (mantém pending transactions em memória)
    // Usa IServiceScopeFactory internamente pra resolver repo
    services.AddSingleton<SettlementService>(sp =>
    {
        var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
        return new SettlementService(scopeFactory);
    });

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
                e.UseMessageRetry(r => r.Intervals(1000, 5000, 30000));
            });
        });
    });

    services.AddHostedService<SettlementBatchWorker>();
});

var host = builder.Build();
host.Run();
