using System.Collections.Concurrent;
using AcquirerFlow.Settlement.Domain.Entities;
using AcquirerFlow.Settlement.Domain.Ports.Out;

namespace AcquirerFlow.Settlement.Worker.Persistence;

public class InMemorySettlementRepository : ISettlementRepository
{
    private readonly ConcurrentDictionary<Guid, SettlementBatch> _store = new();

    public Task SaveAsync(SettlementBatch batch)
    {
        _store.AddOrUpdate(batch.Id, batch, (_, _) => batch);
        return Task.CompletedTask;
    }

    public Task<SettlementBatch?> GetByIdAsync(Guid id)
    {
        _store.TryGetValue(id, out var batch);
        return Task.FromResult(batch);
    }
}
