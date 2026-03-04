using AcquirerFlow.Reconciliation.Domain.Entities;

namespace AcquirerFlow.Reconciliation.Domain.Ports.Out;

public interface IReconciliationRepository
{
    Task SaveAsync(ReconciliationReport report);
    Task<ReconciliationReport?> GetByIdAsync(Guid id);
    Task<List<ReconciliationReport>> GetAllAsync(int page = 1, int size = 20);
    Task<int> CountAsync();
}
