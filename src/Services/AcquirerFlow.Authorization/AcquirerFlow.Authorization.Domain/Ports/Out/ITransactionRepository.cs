using AcquirerFlow.Authorization.Domain.Entities;

namespace AcquirerFlow.Authorization.Domain.Ports.Out;

public interface ITransactionRepository
{
    Task SaveAsync(Transaction transaction);
    Task<Transaction?> GetByIdAsync(Guid id);
    Task<Transaction?> GetByExternalIdAsync(string externalId);
}
