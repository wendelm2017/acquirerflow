using AcquirerFlow.Capture.Application.Services;
using AcquirerFlow.Capture.Domain.Ports.Out;
using AcquirerFlow.Capture.Worker;
using AcquirerFlow.Capture.Worker.Persistence;

var builder = Host.CreateDefaultBuilder(args);

builder.ConfigureServices(services =>
{
    services.AddSingleton<ICaptureRepository, InMemoryCaptureRepository>();
    services.AddSingleton<CaptureService>();
    services.AddHostedService<KafkaCaptureWorker>();
});

var host = builder.Build();
host.Run();
