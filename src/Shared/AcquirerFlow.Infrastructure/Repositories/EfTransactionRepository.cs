using AcquirerFlow.Authorization.Domain.Entities;
using AcquirerFlow.Authorization.Domain.Ports.Out;
using AcquirerFlow.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace AcquirerFlow.Infrastructure.Repositories;

public class EfTransactionRepository : ITransactionRepository
{
    private readonly AcquirerFlowDbContext _db;
    public EfTransactionRepository(AcquirerFlowDbContext db) => _db = db;

    public async Task SaveAsync(Transaction transaction)
    {
        var existing = await _db.Transactions.FindAsync(transaction.Id);
        if (existing is null)
            _db.Transactions.Add(transaction);
        else
            _db.Transactions.Update(transaction);
        await _db.SaveChangesAsync();
    }

    public async Task<Transaction?> GetByIdAsync(Guid id)
        => await _db.Transactions.FirstOrDefaultAsync(t => t.Id == id);

    public async Task<Transaction?> GetByExternalIdAsync(string externalId)
        => await _db.Transactions.FirstOrDefaultAsync(t => t.ExternalId == externalId);
}
