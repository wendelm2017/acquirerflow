namespace AcquirerFlow.Authorization.Domain.ValueObjects;

public sealed record TransactionStatus
{
    public string Value { get; init; } = string.Empty;

    private TransactionStatus() { } // EF Core
    private TransactionStatus(string value) => Value = value;

    public static TransactionStatus Pending => new("PENDING");
    public static TransactionStatus Authorized => new("AUTHORIZED");
    public static TransactionStatus Declined => new("DECLINED");
    public static TransactionStatus Captured => new("CAPTURED");
    public static TransactionStatus Settled => new("SETTLED");
    public static TransactionStatus Cancelled => new("CANCELLED");

    public TransactionStatus Authorize()
    {
        if (Value != "PENDING") throw new InvalidOperationException($"Cannot authorize from {Value}");
        return Authorized;
    }

    public TransactionStatus Decline()
    {
        if (Value != "PENDING") throw new InvalidOperationException($"Cannot decline from {Value}");
        return Declined;
    }

    public TransactionStatus Capture()
    {
        if (Value != "AUTHORIZED") throw new InvalidOperationException($"Cannot capture from {Value}");
        return Captured;
    }

    public override string ToString() => Value;
}
