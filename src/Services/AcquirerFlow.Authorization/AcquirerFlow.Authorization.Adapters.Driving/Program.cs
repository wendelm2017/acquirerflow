using AcquirerFlow.Authorization.CrossCutting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddAuthorizationServices();

var app = builder.Build();

app.MapControllers();

app.Run();
