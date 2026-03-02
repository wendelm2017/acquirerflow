namespace AcquirerFlow.Contracts.Events;

public record SettlementBatchCreated(
    Guid BatchId,
    Guid MerchantId,
    DateTime ReferenceDate,
    int TransactionCount,
    decimal GrossAmount,
    decimal MdrAmount,
    decimal InterchangeAmount,
    decimal SchemeFeeAmount,
    decimal NetAmount,
    DateTime SettlementDate);
