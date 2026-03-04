using AcquirerFlow.Infrastructure.Context;
using AcquirerFlow.Reconciliation.Domain.Entities;
using AcquirerFlow.Reconciliation.Domain.Ports.Out;
using Microsoft.EntityFrameworkCore;

namespace AcquirerFlow.Infrastructure.Repositories;

public class EfReconciliationRepository : IReconciliationRepository
{
    private readonly AcquirerFlowDbContext _context;

    public EfReconciliationRepository(AcquirerFlowDbContext context) => _context = context;

    public async Task SaveAsync(ReconciliationReport report)
    {
        _context.ReconciliationReports.Add(report);
        await _context.SaveChangesAsync();
    }

    public async Task<ReconciliationReport?> GetByIdAsync(Guid id) =>
        await _context.ReconciliationReports
            .Include(r => r.Entries)
            .FirstOrDefaultAsync(r => r.Id == id);

    public async Task<List<ReconciliationReport>> GetAllAsync(int page = 1, int size = 20) =>
        await _context.ReconciliationReports
            .AsNoTracking()
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync();

    public async Task<int> CountAsync() => await _context.ReconciliationReports.CountAsync();
}
