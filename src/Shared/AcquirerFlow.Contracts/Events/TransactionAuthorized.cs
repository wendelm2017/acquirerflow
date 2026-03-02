namespace AcquirerFlow.Contracts.Events;

public record TransactionAuthorized(
    Guid TransactionId,
    Guid MerchantId,
    string CardToken,
    string CardBrand,
    decimal Amount,
    string Currency,
    int Installments,
    string Type,
    string AuthorizationCode,
    DateTime AuthorizedAt);
