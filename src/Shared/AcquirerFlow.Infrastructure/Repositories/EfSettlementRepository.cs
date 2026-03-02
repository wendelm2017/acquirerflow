using AcquirerFlow.Settlement.Domain.Entities;
using AcquirerFlow.Settlement.Domain.Ports.Out;
using AcquirerFlow.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace AcquirerFlow.Infrastructure.Repositories;

public class EfSettlementRepository : ISettlementRepository
{
    private readonly AcquirerFlowDbContext _db;
    public EfSettlementRepository(AcquirerFlowDbContext db) => _db = db;

    public async Task SaveAsync(SettlementBatch batch)
    {
        _db.SettlementBatches.Add(batch);
        await _db.SaveChangesAsync();
    }

    public async Task<SettlementBatch?> GetByIdAsync(Guid id)
        => await _db.SettlementBatches.FirstOrDefaultAsync(b => b.Id == id);
}
