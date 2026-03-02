using AcquirerFlow.Settlement.Application.Services;

namespace AcquirerFlow.Settlement.Worker;

public class SettlementBatchWorker : BackgroundService
{
    private readonly SettlementService _settlementService;
    private readonly ILogger<SettlementBatchWorker> _logger;

    public SettlementBatchWorker(SettlementService settlementService, ILogger<SettlementBatchWorker> logger)
    {
        _settlementService = settlementService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[SETTLEMENT BATCH] Worker started. Processing every 30 seconds.");

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

            try
            {
                var batches = await _settlementService.ProcessBatchesAsync();

                if (batches.Any())
                {
                    foreach (var batch in batches)
                    {
                        _logger.LogInformation(
                            "[SETTLEMENT BATCH] Merchant={MerchantId} | Txns={Count} | Gross={Gross:N2} | MDR={Mdr:N2} | Net={Net:N2} | Settlement={Date:yyyy-MM-dd}",
                            batch.MerchantId, batch.TransactionCount,
                            batch.Fees.GrossAmount, batch.Fees.MdrAmount, batch.Fees.NetAmount,
                            batch.SettlementDate);
                    }
                }
                else
                {
                    _logger.LogInformation("[SETTLEMENT BATCH] No pending transactions to process.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SETTLEMENT BATCH] Error processing batches");
            }
        }
    }
}
