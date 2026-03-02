namespace AcquirerFlow.Authorization.Domain.ValueObjects;

public sealed record TransactionStatus
{
    public string Value { get; }

    private TransactionStatus(string value) => Value = value;

    public static TransactionStatus Pending => new("PENDING");
    public static TransactionStatus Authorized => new("AUTHORIZED");
    public static TransactionStatus Declined => new("DECLINED");
    public static TransactionStatus Captured => new("CAPTURED");
    public static TransactionStatus Settled => new("SETTLED");
    public static TransactionStatus Cancelled => new("CANCELLED");

    public TransactionStatus Authorize()
    {
        if (Value != "PENDING")
            throw new DomainException($"Cannot authorize transaction in status {Value}");
        return Authorized;
    }

    public TransactionStatus Decline()
    {
        if (Value != "PENDING")
            throw new DomainException($"Cannot decline transaction in status {Value}");
        return Declined;
    }

    public TransactionStatus Capture()
    {
        if (Value != "AUTHORIZED")
            throw new DomainException($"Cannot capture transaction in status {Value}");
        return Captured;
    }

    public override string ToString() => Value;
}
