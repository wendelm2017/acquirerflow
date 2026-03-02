using System.Collections.Concurrent;
using AcquirerFlow.Authorization.Domain.Entities;
using AcquirerFlow.Authorization.Domain.Ports.Out;

namespace AcquirerFlow.Authorization.Adapters.Driven.Persistence;

public class InMemoryTransactionRepository : ITransactionRepository
{
    private readonly ConcurrentDictionary<Guid, Transaction> _store = new();

    public Task SaveAsync(Transaction transaction)
    {
        _store.AddOrUpdate(transaction.Id, transaction, (_, _) => transaction);
        return Task.CompletedTask;
    }

    public Task<Transaction?> GetByIdAsync(Guid id)
    {
        _store.TryGetValue(id, out var transaction);
        return Task.FromResult(transaction);
    }

    public Task<Transaction?> GetByExternalIdAsync(string externalId)
    {
        var transaction = _store.Values.FirstOrDefault(t => t.ExternalId == externalId);
        return Task.FromResult(transaction);
    }
}
