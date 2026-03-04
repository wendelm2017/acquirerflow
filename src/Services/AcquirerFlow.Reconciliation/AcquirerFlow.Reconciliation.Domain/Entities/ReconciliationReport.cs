namespace AcquirerFlow.Reconciliation.Domain.Entities;

public sealed class ReconciliationReport
{
    public Guid Id { get; private set; }
    public DateTime ReferenceDate { get; private set; }
    public string Status { get; private set; } = string.Empty;
    public int TotalTransactions { get; private set; }
    public int TotalCaptures { get; private set; }
    public int TotalSettlements { get; private set; }
    public int DiscrepancyCount { get; private set; }
    public decimal TotalAuthorizedAmount { get; private set; }
    public decimal TotalCapturedAmount { get; private set; }
    public decimal TotalSettledGrossAmount { get; private set; }
    public decimal TotalSettledNetAmount { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    private readonly List<ReconciliationEntry> _entries = new();
    public IReadOnlyList<ReconciliationEntry> Entries => _entries.AsReadOnly();

    private ReconciliationReport() { } // EF Core

    public static ReconciliationReport Create(DateTime referenceDate)
    {
        return new ReconciliationReport
        {
            Id = Guid.NewGuid(),
            ReferenceDate = referenceDate,
            Status = "RUNNING",
            CreatedAt = DateTime.UtcNow
        };
    }

    public void SetTotals(
        int totalTransactions, int totalCaptures, int totalSettlements,
        decimal totalAuthorizedAmount, decimal totalCapturedAmount,
        decimal totalSettledGrossAmount, decimal totalSettledNetAmount)
    {
        TotalTransactions = totalTransactions;
        TotalCaptures = totalCaptures;
        TotalSettlements = totalSettlements;
        TotalAuthorizedAmount = totalAuthorizedAmount;
        TotalCapturedAmount = totalCapturedAmount;
        TotalSettledGrossAmount = totalSettledGrossAmount;
        TotalSettledNetAmount = totalSettledNetAmount;
    }

    public void AddEntry(ReconciliationEntry entry)
    {
        if (Status != "RUNNING")
            throw new InvalidOperationException("Cannot add entries to a completed report");
        _entries.Add(entry);
    }

    public void Complete()
    {
        DiscrepancyCount = _entries.Count;
        Status = _entries.Count == 0 ? "RECONCILED" : "DISCREPANCIES_FOUND";
        CompletedAt = DateTime.UtcNow;
    }
}
