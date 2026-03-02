using AcquirerFlow.Authorization.Domain.ValueObjects;

namespace AcquirerFlow.Authorization.Domain.Entities;

public sealed class Transaction
{
    public Guid Id { get; private set; }
    public string ExternalId { get; private set; } = string.Empty;
    public Guid MerchantId { get; private set; }
    public CardToken Card { get; private set; } = null!;
    public Money Amount { get; private set; } = null!;
    public int Installments { get; private set; }
    public string Type { get; private set; } = string.Empty;
    public TransactionStatus Status { get; private set; } = null!;
    public string? AuthorizationCode { get; private set; }
    public string? DeclinedReason { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? AuthorizedAt { get; private set; }
    public DateTime? CapturedAt { get; private set; }

    private Transaction() { } // EF Core

    public static Transaction Create(
        string externalId,
        Guid merchantId,
        CardToken card,
        Money amount,
        int installments,
        string type)
    {
        if (string.IsNullOrWhiteSpace(externalId))
            throw new DomainException("ExternalId is required");

        if (merchantId == Guid.Empty)
            throw new DomainException("MerchantId is required");

        var validTypes = new[] { "DEBIT", "CREDIT" };
        var normalizedType = type.ToUpperInvariant();
        if (!validTypes.Contains(normalizedType))
            throw new DomainException($"Invalid transaction type: {type}");

        if (normalizedType == "DEBIT" && installments > 1)
            throw new DomainException("Debit transactions cannot have installments");

        if (installments < 1 || installments > 12)
            throw new DomainException("Installments must be between 1 and 12");

        return new Transaction
        {
            Id = Guid.NewGuid(),
            ExternalId = externalId,
            MerchantId = merchantId,
            Card = card,
            Amount = amount,
            Installments = installments,
            Type = normalizedType,
            Status = TransactionStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Authorize(string authorizationCode)
    {
        if (string.IsNullOrWhiteSpace(authorizationCode))
            throw new DomainException("Authorization code is required");

        Status = Status.Authorize();
        AuthorizationCode = authorizationCode;
        AuthorizedAt = DateTime.UtcNow;
    }

    public void Decline(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Decline reason is required");

        Status = Status.Decline();
        DeclinedReason = reason;
    }

    public void Capture()
    {
        Status = Status.Capture();
        CapturedAt = DateTime.UtcNow;
    }
}
