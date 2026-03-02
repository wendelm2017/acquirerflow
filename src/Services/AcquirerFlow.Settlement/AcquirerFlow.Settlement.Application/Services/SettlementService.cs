using AcquirerFlow.Contracts.Events;
using AcquirerFlow.Settlement.Domain.Entities;
using AcquirerFlow.Settlement.Domain.Ports.Out;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AcquirerFlow.Settlement.Application.Services;

public class SettlementService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly Dictionary<Guid, List<TransactionCaptured>> _pending = new();
    private readonly ILogger<SettlementService> _logger;

    public SettlementService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
        using var scope = scopeFactory.CreateScope();
        _logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<SettlementService>();
    }

    // Constructor for unit tests
    public SettlementService(ISettlementRepository repository)
    {
        _scopeFactory = null!;
        _logger = null!;
    }

    private ISettlementRepository? _testRepo;

    public void AccumulateCapture(TransactionCaptured captured)
    {
        if (!_pending.ContainsKey(captured.MerchantId))
            _pending[captured.MerchantId] = new List<TransactionCaptured>();

        _pending[captured.MerchantId].Add(captured);

        Console.WriteLine($"[SETTLEMENT] Accumulated: {captured.Currency} {captured.CapturedAmount:F2} | Merchant: {captured.MerchantId} | Pending: {_pending[captured.MerchantId].Count}");
    }

    public async Task<List<SettlementBatch>> ProcessBatchesAsync()
    {
        if (_pending.Count == 0)
            return new List<SettlementBatch>();

        var batches = new List<SettlementBatch>();

        // Resolve repo via scope or use test repo
        ISettlementRepository repo;
        IServiceScope? scope = null;

        if (_scopeFactory is not null)
        {
            scope = _scopeFactory.CreateScope();
            repo = scope.ServiceProvider.GetRequiredService<ISettlementRepository>();
        }
        else
        {
            repo = _testRepo!;
        }

        try
        {
            foreach (var (merchantId, captures) in _pending)
            {
                var batch = SettlementBatch.Create(merchantId, DateTime.UtcNow.Date);
                foreach (var c in captures)
                    batch.AddItem(c.TransactionId, c.CapturedAmount, c.Currency, c.Type, c.Installments);

                batch.Process();
                await repo.SaveAsync(batch);
                batches.Add(batch);

                Console.WriteLine($"[SETTLEMENT] Batch {batch.Id} processed: {captures.Count} txns | Gross: {batch.Fees.GrossAmount:F2} | MDR: {batch.Fees.MdrAmount:F2} | Net: {batch.Fees.NetAmount:F2}");
            }

            _pending.Clear();
        }
        finally
        {
            scope?.Dispose();
        }

        return batches;
    }

    // For unit tests
    public void SetTestRepository(ISettlementRepository repo) => _testRepo = repo;
}
