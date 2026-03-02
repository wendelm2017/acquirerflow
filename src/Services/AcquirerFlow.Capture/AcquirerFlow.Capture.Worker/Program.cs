using AcquirerFlow.Capture.Application.Services;
using AcquirerFlow.Capture.Domain.Ports.Out;
using AcquirerFlow.Capture.Worker;
using AcquirerFlow.Capture.Worker.Persistence;
using MassTransit;

var builder = Host.CreateDefaultBuilder(args);

builder.ConfigureServices(services =>
{
    services.AddSingleton<ICaptureRepository, InMemoryCaptureRepository>();
    services.AddSingleton<CaptureService>();

    // MassTransit publisher (só publica, não consome)
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
