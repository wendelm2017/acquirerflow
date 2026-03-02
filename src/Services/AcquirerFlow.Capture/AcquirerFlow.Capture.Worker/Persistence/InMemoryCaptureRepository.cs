using System.Collections.Concurrent;
using AcquirerFlow.Capture.Domain.Entities;
using AcquirerFlow.Capture.Domain.Ports.Out;

namespace AcquirerFlow.Capture.Worker.Persistence;

public class InMemoryCaptureRepository : ICaptureRepository
{
    private readonly ConcurrentDictionary<Guid, CapturedTransaction> _store = new();

    public Task SaveAsync(CapturedTransaction transaction)
    {
        _store.TryAdd(transaction.OriginalTransactionId, transaction);
        return Task.CompletedTask;
    }

    public Task<CapturedTransaction?> GetByTransactionIdAsync(Guid transactionId)
    {
        _store.TryGetValue(transactionId, out var tx);
        return Task.FromResult(tx);
    }

    public Task<bool> ExistsAsync(Guid transactionId)
    {
        return Task.FromResult(_store.ContainsKey(transactionId));
    }
}
