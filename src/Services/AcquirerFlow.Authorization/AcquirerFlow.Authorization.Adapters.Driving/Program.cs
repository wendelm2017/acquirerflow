using AcquirerFlow.Authorization.CrossCutting;
using AcquirerFlow.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "AcquirerFlow API",
        Version = "v1",
        Description = "Acquirer/Sub-Acquirer platform - Authorization, Capture, Settlement & Reconciliation",
        Contact = new() { Name = "Wendel Machado", Email = "wendelm2017@gmail.com" }
    });
});

builder.Services.AddAuthorizationServices();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=localhost,1433;Database=AcquirerFlowDb;User Id=sa;Password=AcquirerFlow@2024;TrustServerCertificate=True";
builder.Services.AddAcquirerFlowInfrastructure(connectionString);

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "AcquirerFlow API v1");
    c.RoutePrefix = string.Empty; // Swagger na raiz
});

app.MapControllers();
app.Run();
