using AcquirerFlow.Contracts.Events;
using AcquirerFlow.Settlement.Domain.Entities;
using AcquirerFlow.Settlement.Domain.Ports.Out;

namespace AcquirerFlow.Settlement.Application.Services;

public class SettlementService
{
    private readonly ISettlementRepository _repository;

    // Acumula transações capturadas até o batch rodar
    private readonly Dictionary<Guid, List<TransactionCaptured>> _pendingByMerchant = new();

    public SettlementService(ISettlementRepository repository)
    {
        _repository = repository;
    }

    public void AccumulateCapture(TransactionCaptured captured)
    {
        if (!_pendingByMerchant.ContainsKey(captured.MerchantId))
            _pendingByMerchant[captured.MerchantId] = new List<TransactionCaptured>();

        _pendingByMerchant[captured.MerchantId].Add(captured);

        Console.WriteLine($"[SETTLEMENT] Accumulated: {captured.Currency} {captured.CapturedAmount:N2} | Merchant: {captured.MerchantId} | Pending: {_pendingByMerchant[captured.MerchantId].Count}");
    }

    public async Task<List<SettlementBatch>> ProcessBatchesAsync()
    {
        var batches = new List<SettlementBatch>();

        foreach (var (merchantId, captures) in _pendingByMerchant)
        {
            if (!captures.Any()) continue;

            var batch = SettlementBatch.Create(merchantId, DateTime.UtcNow.Date);

            foreach (var capture in captures)
            {
                batch.AddItem(
                    capture.TransactionId,
                    capture.CapturedAmount,
                    capture.Currency,
                    capture.Type,
                    capture.Installments);
            }

            batch.Process();
            await _repository.SaveAsync(batch);
            batches.Add(batch);

            Console.WriteLine($"[SETTLEMENT] Batch {batch.Id} processed: {captures.Count} txns | Gross: {batch.Fees.GrossAmount:N2} | MDR: {batch.Fees.MdrAmount:N2} | Net: {batch.Fees.NetAmount:N2}");
        }

        _pendingByMerchant.Clear();
        return batches;
    }
}
