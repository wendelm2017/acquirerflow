namespace AcquirerFlow.Contracts.Events;

public record TransactionCaptured
{
    public Guid TransactionId { get; init; }
    public Guid MerchantId { get; init; }
    public string CardToken { get; init; } = string.Empty;
    public string CardBrand { get; init; } = string.Empty;
    public decimal CapturedAmount { get; init; }
    public string Currency { get; init; } = string.Empty;
    public int Installments { get; init; }
    public string Type { get; init; } = string.Empty;
    public string AuthorizationCode { get; init; } = string.Empty;
    public DateTime CapturedAt { get; init; }

    public TransactionCaptured() { }

    public TransactionCaptured(Guid transactionId, Guid merchantId, string cardToken, string cardBrand,
        decimal capturedAmount, string currency, int installments, string type,
        string authorizationCode, DateTime capturedAt)
    {
        TransactionId = transactionId;
        MerchantId = merchantId;
        CardToken = cardToken;
        CardBrand = cardBrand;
        CapturedAmount = capturedAmount;
        Currency = currency;
        Installments = installments;
        Type = type;
        AuthorizationCode = authorizationCode;
        CapturedAt = capturedAt;
    }
}
