using AcquirerFlow.Reconciliation.Application.Services;

namespace AcquirerFlow.Reconciliation.Worker;

public class ReconciliationBackgroundWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ReconciliationBackgroundWorker> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(5);

    public ReconciliationBackgroundWorker(IServiceScopeFactory scopeFactory, ILogger<ReconciliationBackgroundWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[RECONCILIATION] Worker started. Running every {Interval}.", _interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<ReconciliationService>();
                var report = await service.RunReconciliationAsync();

                if (report.DiscrepancyCount > 0)
                    _logger.LogWarning("[RECONCILIATION] {Count} discrepancies found for {Date}. Report: {Id}",
                        report.DiscrepancyCount, report.ReferenceDate, report.Id);
                else
                    _logger.LogInformation("[RECONCILIATION] All reconciled for {Date}. Tx:{Tx} Cap:{Cap} Stl:{Stl}",
                        report.ReferenceDate, report.TotalTransactions, report.TotalCaptures, report.TotalSettlements);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RECONCILIATION] Error during reconciliation run");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }
}
