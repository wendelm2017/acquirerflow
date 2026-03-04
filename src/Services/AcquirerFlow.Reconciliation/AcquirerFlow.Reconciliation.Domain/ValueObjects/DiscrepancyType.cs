namespace AcquirerFlow.Reconciliation.Domain.ValueObjects;

/// <summary>
/// Value Object representing the type of reconciliation discrepancy found.
/// </summary>
public sealed record DiscrepancyType
{
    public string Value { get; init; }

    private DiscrepancyType() { Value = string.Empty; }
    private DiscrepancyType(string value) => Value = value;

    public static DiscrepancyType MissingCapture => new("MISSING_CAPTURE");
    public static DiscrepancyType MissingSettlement => new("MISSING_SETTLEMENT");
    public static DiscrepancyType AmountMismatch => new("AMOUNT_MISMATCH");
    public static DiscrepancyType OrphanCapture => new("ORPHAN_CAPTURE");
    public static DiscrepancyType OrphanSettlement => new("ORPHAN_SETTLEMENT");
    public static DiscrepancyType DeclinedButCaptured => new("DECLINED_BUT_CAPTURED");

    public override string ToString() => Value;
}
