using AcquirerFlow.Reconciliation.Application.DTOs;
using AcquirerFlow.Reconciliation.Domain.Entities;
using AcquirerFlow.Reconciliation.Domain.Ports.Out;
using AcquirerFlow.Reconciliation.Domain.ValueObjects;

namespace AcquirerFlow.Reconciliation.Application.Services;

public class ReconciliationService
{
    private readonly IReconciliationRepository _repository;
    private readonly ITransactionDataSource _dataSource;

    public ReconciliationService(IReconciliationRepository repository, ITransactionDataSource dataSource)
    {
        _repository = repository;
        _dataSource = dataSource;
    }

    public async Task<ReconciliationReportDto> RunReconciliationAsync(DateTime? referenceDate = null)
    {
        var refDate = referenceDate ?? DateTime.UtcNow.Date;
        var report = ReconciliationReport.Create(refDate);

        // Load snapshots from all 3 data sources
        var transactions = await _dataSource.GetAuthorizedTransactionsAsync(refDate);
        var captures = await _dataSource.GetCapturesAsync(refDate);
        var settlements = await _dataSource.GetSettlementBatchesAsync(refDate);

        // Lookup structures
        var capturesByTxId = captures.ToDictionary(c => c.OriginalTransactionId, c => c);
        var capturedTxIds = new HashSet<Guid>(captures.Select(c => c.OriginalTransactionId));
        var txById = transactions.ToDictionary(t => t.Id, t => t);

        // Set totals
        report.SetTotals(
            transactions.Count, captures.Count, settlements.Count,
            transactions.Where(t => t.Status == "AUTHORIZED").Sum(t => t.Amount),
            captures.Sum(c => c.CapturedAmount),
            settlements.Sum(s => s.GrossAmount),
            settlements.Sum(s => s.NetAmount));

        // CHECK 1: Authorized but not captured
        foreach (var tx in transactions.Where(t => t.Status == "AUTHORIZED"))
        {
            if (!capturedTxIds.Contains(tx.Id))
            {
                report.AddEntry(ReconciliationEntry.Create(
                    report.Id, tx.MerchantId, DiscrepancyType.MissingCapture,
                    $"Transaction {tx.Id} authorized for {tx.Currency} {tx.Amount:N2} but not captured",
                    transactionId: tx.Id, expectedAmount: tx.Amount));
            }
        }

        // CHECK 2: Amount mismatch authorization vs capture
        foreach (var capture in captures)
        {
            if (txById.TryGetValue(capture.OriginalTransactionId, out var tx) && tx.Amount != capture.CapturedAmount)
            {
                report.AddEntry(ReconciliationEntry.Create(
                    report.Id, capture.MerchantId, DiscrepancyType.AmountMismatch,
                    $"Tx {tx.Id}: authorized {tx.Currency} {tx.Amount:N2}, captured {capture.CapturedAmount:N2}",
                    transactionId: tx.Id, captureId: capture.Id,
                    expectedAmount: tx.Amount, actualAmount: capture.CapturedAmount));
            }
        }

        // CHECK 3: Orphan captures (no matching authorization)
        foreach (var capture in captures)
        {
            if (!txById.ContainsKey(capture.OriginalTransactionId))
            {
                report.AddEntry(ReconciliationEntry.Create(
                    report.Id, capture.MerchantId, DiscrepancyType.OrphanCapture,
                    $"Capture {capture.Id} references missing transaction {capture.OriginalTransactionId}",
                    captureId: capture.Id, actualAmount: capture.CapturedAmount));
            }
        }

        // CHECK 4: Declined but captured
        foreach (var tx in transactions.Where(t => t.Status == "DECLINED"))
        {
            if (capturedTxIds.Contains(tx.Id))
            {
                report.AddEntry(ReconciliationEntry.Create(
                    report.Id, tx.MerchantId, DiscrepancyType.DeclinedButCaptured,
                    $"Transaction {tx.Id} was DECLINED but appears in captures",
                    transactionId: tx.Id, captureId: capturesByTxId[tx.Id].Id));
            }
        }

        // CHECK 5: Captured total vs settled gross total per merchant
        var capturedByMerchant = captures.GroupBy(c => c.MerchantId)
            .ToDictionary(g => g.Key, g => g.Sum(c => c.CapturedAmount));
        var settledByMerchant = settlements.GroupBy(s => s.MerchantId)
            .ToDictionary(g => g.Key, g => g.Sum(s => s.GrossAmount));

        foreach (var (merchantId, capturedTotal) in capturedByMerchant)
        {
            settledByMerchant.TryGetValue(merchantId, out var settledTotal);
            if (Math.Abs(capturedTotal - settledTotal) > 0.01m)
            {
                report.AddEntry(ReconciliationEntry.Create(
                    report.Id, merchantId, DiscrepancyType.MissingSettlement,
                    $"Merchant {merchantId}: captured {capturedTotal:N2} vs settled {settledTotal:N2} (diff: {capturedTotal - settledTotal:N2})",
                    expectedAmount: capturedTotal, actualAmount: settledTotal));
            }
        }

        report.Complete();
        await _repository.SaveAsync(report);
        return MapToDto(report);
    }

    public async Task<ReconciliationReportDto?> GetReportAsync(Guid id)
    {
        var report = await _repository.GetByIdAsync(id);
        return report is null ? null : MapToDto(report);
    }

    public async Task<object> GetReportsAsync(int page = 1, int size = 20)
    {
        var reports = await _repository.GetAllAsync(page, size);
        var total = await _repository.CountAsync();
        return new
        {
            total, page, size,
            items = reports.Select(r => new ReconciliationSummaryDto(
                r.Id, r.ReferenceDate, r.Status,
                r.TotalTransactions, r.TotalCaptures, r.TotalSettlements,
                r.DiscrepancyCount, r.CreatedAt)).ToList()
        };
    }

    private static ReconciliationReportDto MapToDto(ReconciliationReport r) =>
        new(r.Id, r.ReferenceDate, r.Status,
            r.TotalTransactions, r.TotalCaptures, r.TotalSettlements, r.DiscrepancyCount,
            r.TotalAuthorizedAmount, r.TotalCapturedAmount,
            r.TotalSettledGrossAmount, r.TotalSettledNetAmount,
            r.CreatedAt, r.CompletedAt,
            r.Entries.Select(e => new ReconciliationEntryDto(
                e.Id, e.TransactionId, e.CaptureId, e.SettlementBatchId,
                e.MerchantId, e.Discrepancy.Value,
                e.ExpectedAmount, e.ActualAmount,
                e.Details, e.DetectedAt)).ToList());
}
