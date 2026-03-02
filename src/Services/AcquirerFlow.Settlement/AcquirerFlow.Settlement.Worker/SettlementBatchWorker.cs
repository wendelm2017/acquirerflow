using AcquirerFlow.Settlement.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AcquirerFlow.Settlement.Worker;

public class SettlementBatchWorker : BackgroundService
{
    private readonly ILogger<SettlementBatchWorker> _logger;
    private readonly SettlementService _settlementService;

    public SettlementBatchWorker(ILogger<SettlementBatchWorker> logger, SettlementService settlementService)
    {
        _logger = logger;
        _settlementService = settlementService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[SETTLEMENT BATCH] Worker started. Processing every 30 seconds.");

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(30_000, stoppingToken);

            try
            {
                var batches = await _settlementService.ProcessBatchesAsync();
                if (batches.Count == 0)
                {
                    _logger.LogInformation("[SETTLEMENT BATCH] No pending transactions to process.");
                    continue;
                }

                foreach (var batch in batches)
                {
                    _logger.LogInformation(
                        "[SETTLEMENT BATCH] Merchant={m} | Txns={t} | Gross={g:F2} | MDR={mdr:F2} | Net={n:F2} | Settlement={s:yyyy-MM-dd}",
                        batch.MerchantId, batch.TransactionCount,
                        batch.Fees.GrossAmount, batch.Fees.MdrAmount,
                        batch.Fees.NetAmount, batch.SettlementDate);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SETTLEMENT BATCH] Error processing batches");
            }
        }
    }
}
