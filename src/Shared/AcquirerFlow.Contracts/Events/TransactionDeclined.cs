namespace AcquirerFlow.Contracts.Events;

public record TransactionDeclined(
    Guid TransactionId,
    Guid MerchantId,
    string CardToken,
    decimal Amount,
    string Reason,
    DateTime DeclinedAt);
