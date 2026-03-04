namespace AcquirerFlow.Reconciliation.Domain.Ports.Out;

public interface ITransactionDataSource
{
    Task<List<TransactionSnapshot>> GetAuthorizedTransactionsAsync(DateTime referenceDate);
    Task<List<CaptureSnapshot>> GetCapturesAsync(DateTime referenceDate);
    Task<List<SettlementSnapshot>> GetSettlementBatchesAsync(DateTime referenceDate);
}

public record TransactionSnapshot(
    Guid Id, Guid MerchantId, string Status, decimal Amount, string Currency, DateTime CreatedAt);

public record CaptureSnapshot(
    Guid Id, Guid OriginalTransactionId, Guid MerchantId, decimal CapturedAmount, string Currency, DateTime CapturedAt);

public record SettlementSnapshot(
    Guid Id, Guid MerchantId, int TransactionCount, decimal GrossAmount, decimal NetAmount, string Status, DateTime ReferenceDate);
