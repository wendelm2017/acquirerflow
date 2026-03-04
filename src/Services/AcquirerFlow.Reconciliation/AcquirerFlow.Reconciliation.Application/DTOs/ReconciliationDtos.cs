namespace AcquirerFlow.Reconciliation.Application.DTOs;

public record ReconciliationReportDto(
    Guid Id, DateTime ReferenceDate, string Status,
    int TotalTransactions, int TotalCaptures, int TotalSettlements, int DiscrepancyCount,
    decimal TotalAuthorizedAmount, decimal TotalCapturedAmount,
    decimal TotalSettledGrossAmount, decimal TotalSettledNetAmount,
    DateTime CreatedAt, DateTime? CompletedAt,
    List<ReconciliationEntryDto> Entries);

public record ReconciliationEntryDto(
    Guid Id, Guid? TransactionId, Guid? CaptureId, Guid? SettlementBatchId,
    Guid MerchantId, string Discrepancy,
    decimal? ExpectedAmount, decimal? ActualAmount,
    string Details, DateTime DetectedAt);

public record ReconciliationSummaryDto(
    Guid Id, DateTime ReferenceDate, string Status,
    int TotalTransactions, int TotalCaptures, int TotalSettlements,
    int DiscrepancyCount, DateTime CreatedAt);
