using AcquirerFlow.Authorization.Adapters.Driven.FraudCheck;
using AcquirerFlow.Authorization.Adapters.Driven.Messaging;
using AcquirerFlow.Authorization.Adapters.Driven.Persistence;
using AcquirerFlow.Authorization.Adapters.Driven.TokenService;
using AcquirerFlow.Authorization.Application.Services;
using AcquirerFlow.Authorization.Domain.Ports.Out;
using Microsoft.Extensions.DependencyInjection;

namespace AcquirerFlow.Authorization.CrossCutting;

public static class DependencyInjection
{
    public static IServiceCollection AddAuthorizationServices(this IServiceCollection services)
    {
        // Ports Out — adapters driven
        services.AddSingleton<ITransactionRepository, InMemoryTransactionRepository>();
        services.AddScoped<ICardTokenizer, FakeCardTokenizer>();
        services.AddScoped<IFraudChecker, FakeFraudChecker>();
        services.AddSingleton<IEventPublisher>(new KafkaEventPublisher("localhost:9092"));

        // Application service
        services.AddScoped<AuthorizationAppService>();

        return services;
    }
}
