using AcquirerFlow.Infrastructure;
using AcquirerFlow.Reconciliation.Application.Services;
using AcquirerFlow.Reconciliation.Worker;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "AcquirerFlow Reconciliation API",
        Version = "v1",
        Description = "Reconciliation Service - Cross-references Authorization, Capture, and Settlement data",
        Contact = new() { Name = "Wendel Machado", Email = "wendelm2017@gmail.com" }
    });
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=localhost,1433;Database=AcquirerFlowDb;User Id=sa;Password=AcquirerFlow@2024;TrustServerCertificate=True";
builder.Services.AddAcquirerFlowInfrastructure(connectionString);
builder.Services.AddScoped<ReconciliationService>();
builder.Services.AddHostedService<ReconciliationBackgroundWorker>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "AcquirerFlow Reconciliation v1");
    c.RoutePrefix = string.Empty;
});
app.MapControllers();
app.Run();
