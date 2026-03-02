using AcquirerFlow.Authorization.Domain.Ports.Out;
using AcquirerFlow.Capture.Domain.Ports.Out;
using AcquirerFlow.Settlement.Domain.Ports.Out;
using AcquirerFlow.Infrastructure.Context;
using AcquirerFlow.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AcquirerFlow.Infrastructure;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddAcquirerFlowInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<AcquirerFlowDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<ITransactionRepository, EfTransactionRepository>();
        services.AddScoped<ICaptureRepository, EfCaptureRepository>();
        services.AddScoped<ISettlementRepository, EfSettlementRepository>();

        return services;
    }
}
