using AcquirerFlow.Infrastructure.Context;
using AcquirerFlow.Reconciliation.Domain.Ports.Out;
using Microsoft.EntityFrameworkCore;

namespace AcquirerFlow.Infrastructure.Repositories;

public class EfTransactionDataSource : ITransactionDataSource
{
    private readonly AcquirerFlowDbContext _context;

    public EfTransactionDataSource(AcquirerFlowDbContext context) => _context = context;

    public async Task<List<TransactionSnapshot>> GetAuthorizedTransactionsAsync(DateTime referenceDate) =>
        await _context.Transactions
            .AsNoTracking()
            .Where(t => t.CreatedAt.Date <= referenceDate.Date)
            .Select(t => new TransactionSnapshot(
                t.Id, t.MerchantId, t.Status.Value,
                t.Amount.Amount, t.Amount.Currency, t.CreatedAt))
            .ToListAsync();

    public async Task<List<CaptureSnapshot>> GetCapturesAsync(DateTime referenceDate) =>
        await _context.CapturedTransactions
            .AsNoTracking()
            .Where(c => c.CapturedAt.Date <= referenceDate.Date)
            .Select(c => new CaptureSnapshot(
                c.Id, c.OriginalTransactionId, c.MerchantId,
                c.CapturedAmount, c.Currency, c.CapturedAt))
            .ToListAsync();

    public async Task<List<SettlementSnapshot>> GetSettlementBatchesAsync(DateTime referenceDate) =>
        await _context.SettlementBatches
            .AsNoTracking()
            .Where(s => s.ReferenceDate.Date <= referenceDate.Date)
            .Select(s => new SettlementSnapshot(
                s.Id, s.MerchantId, s.TransactionCount,
                s.Fees.GrossAmount, s.Fees.NetAmount,
                s.Status, s.ReferenceDate))
            .ToListAsync();
}
