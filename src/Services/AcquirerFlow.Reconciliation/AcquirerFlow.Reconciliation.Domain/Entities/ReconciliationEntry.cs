using AcquirerFlow.Reconciliation.Domain.ValueObjects;

namespace AcquirerFlow.Reconciliation.Domain.Entities;

public sealed class ReconciliationEntry
{
    public Guid Id { get; private set; }
    public Guid ReportId { get; private set; }
    public Guid? TransactionId { get; private set; }
    public Guid? CaptureId { get; private set; }
    public Guid? SettlementBatchId { get; private set; }
    public Guid MerchantId { get; private set; }
    public DiscrepancyType Discrepancy { get; private set; } = null!;
    public decimal? ExpectedAmount { get; private set; }
    public decimal? ActualAmount { get; private set; }
    public string Details { get; private set; } = string.Empty;
    public DateTime DetectedAt { get; private set; }

    private ReconciliationEntry() { } // EF Core

    public static ReconciliationEntry Create(
        Guid reportId, Guid merchantId, DiscrepancyType discrepancy, string details,
        Guid? transactionId = null, Guid? captureId = null, Guid? settlementBatchId = null,
        decimal? expectedAmount = null, decimal? actualAmount = null)
    {
        return new ReconciliationEntry
        {
            Id = Guid.NewGuid(),
            ReportId = reportId,
            MerchantId = merchantId,
            Discrepancy = discrepancy,
            Details = details,
            TransactionId = transactionId,
            CaptureId = captureId,
            SettlementBatchId = settlementBatchId,
            ExpectedAmount = expectedAmount,
            ActualAmount = actualAmount,
            DetectedAt = DateTime.UtcNow
        };
    }
}
