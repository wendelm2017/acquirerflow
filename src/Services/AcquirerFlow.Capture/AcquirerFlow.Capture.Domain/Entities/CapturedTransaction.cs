namespace AcquirerFlow.Capture.Domain.Entities;

public sealed class CapturedTransaction
{
    public Guid Id { get; private set; }
    public Guid OriginalTransactionId { get; private set; }
    public Guid MerchantId { get; private set; }
    public string CardToken { get; private set; } = string.Empty;
    public string CardBrand { get; private set; } = string.Empty;
    public decimal AuthorizedAmount { get; private set; }
    public decimal CapturedAmount { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public int Installments { get; private set; }
    public string Type { get; private set; } = string.Empty;
    public string AuthorizationCode { get; private set; } = string.Empty;
    public string Status { get; private set; } = string.Empty;
    public DateTime AuthorizedAt { get; private set; }
    public DateTime CapturedAt { get; private set; }

    private CapturedTransaction() { }

    public static CapturedTransaction CreateFromAuthorization(
        Guid transactionId,
        Guid merchantId,
        string cardToken,
        string cardBrand,
        decimal amount,
        string currency,
        int installments,
        string type,
        string authorizationCode,
        DateTime authorizedAt,
        decimal? partialCaptureAmount = null)
    {
        var captureAmount = partialCaptureAmount ?? amount;

        if (captureAmount <= 0)
            throw new InvalidOperationException("Capture amount must be positive");

        if (captureAmount > amount)
            throw new InvalidOperationException("Capture amount cannot exceed authorized amount");

        return new CapturedTransaction
        {
            Id = Guid.NewGuid(),
            OriginalTransactionId = transactionId,
            MerchantId = merchantId,
            CardToken = cardToken,
            CardBrand = cardBrand,
            AuthorizedAmount = amount,
            CapturedAmount = captureAmount,
            Currency = currency,
            Installments = installments,
            Type = type,
            AuthorizationCode = authorizationCode,
            Status = "CAPTURED",
            AuthorizedAt = authorizedAt,
            CapturedAt = DateTime.UtcNow
        };
    }
}
