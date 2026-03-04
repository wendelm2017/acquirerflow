using AcquirerFlow.Settlement.Domain.Entities;

namespace AcquirerFlow.Settlement.Domain.Ports.Out;

public interface ISettlementRepository
{
    Task SaveAsync(SettlementBatch batch);
    Task<SettlementBatch?> GetByIdAsync(Guid id);
}
