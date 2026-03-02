using AcquirerFlow.Capture.Domain.Entities;
using AcquirerFlow.Capture.Domain.Ports.Out;
using AcquirerFlow.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace AcquirerFlow.Infrastructure.Repositories;

public class EfCaptureRepository : ICaptureRepository
{
    private readonly AcquirerFlowDbContext _db;
    public EfCaptureRepository(AcquirerFlowDbContext db) => _db = db;

    public async Task SaveAsync(CapturedTransaction transaction)
    {
        _db.CapturedTransactions.Add(transaction);
        await _db.SaveChangesAsync();
    }

    public async Task<CapturedTransaction?> GetByTransactionIdAsync(Guid transactionId)
        => await _db.CapturedTransactions
            .FirstOrDefaultAsync(t => t.OriginalTransactionId == transactionId);

    public async Task<bool> ExistsAsync(Guid transactionId)
        => await _db.CapturedTransactions
            .AnyAsync(t => t.OriginalTransactionId == transactionId);
}
