using AcquirerFlow.Capture.Application.Services;
using AcquirerFlow.Capture.Worker;
using AcquirerFlow.Infrastructure;
using MassTransit;

var builder = Host.CreateDefaultBuilder(args);

builder.ConfigureServices(services =>
{
    var connectionString = "Server=localhost,1433;Database=AcquirerFlowDb;User Id=sa;Password=AcquirerFlow@2024;TrustServerCertificate=True";
    services.AddAcquirerFlowInfrastructure(connectionString);
    services.AddScoped<CaptureService>();

    services.AddMassTransit(x =>
    {
        x.UsingRabbitMq((context, cfg) =>
        {
            cfg.Host("localhost", "/", h =>
            {
                h.Username("admin");
                h.Password("AcquirerFlow@2024");
            });
        });
    });

    services.AddHostedService<KafkaCaptureWorker>();
});

var host = builder.Build();
host.Run();
