using AcquirerFlow.Settlement.Domain.ValueObjects;

namespace AcquirerFlow.Settlement.Domain.Entities;

public sealed class SettlementBatch
{
    public Guid Id { get; private set; }
    public Guid MerchantId { get; private set; }
    public DateTime ReferenceDate { get; private set; }
    public FeeBreakdown Fees { get; private set; } = null!;
    public int TransactionCount { get; private set; }
    public string Status { get; private set; } = string.Empty;
    public DateTime? SettlementDate { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private readonly List<SettlementItem> _items = new();
    public IReadOnlyCollection<SettlementItem> Items => _items.AsReadOnly();

    private SettlementBatch() { }

    public static SettlementBatch Create(Guid merchantId, DateTime referenceDate, decimal mdrRate = 0.025m)
    {
        return new SettlementBatch
        {
            Id = Guid.NewGuid(),
            MerchantId = merchantId,
            ReferenceDate = referenceDate,
            Status = "PENDING",
            CreatedAt = DateTime.UtcNow
        };
    }

    public void AddItem(Guid transactionId, decimal amount, string currency, string type, int installments)
    {
        var item = new SettlementItem(Guid.NewGuid(), transactionId, amount, currency, type, installments);
        _items.Add(item);
    }

    public void Process(decimal mdrRate = 0.025m, decimal interchangeRate = 0.015m, decimal schemeFeeRate = 0.003m)
    {
        if (!_items.Any())
            throw new InvalidOperationException("Cannot process empty settlement batch");

        var grossAmount = _items.Sum(i => i.Amount);
        Fees = FeeBreakdown.Calculate(grossAmount, mdrRate, interchangeRate, schemeFeeRate);
        TransactionCount = _items.Count;
        Status = "PROCESSED";
        SettlementDate = ReferenceDate.AddDays(1); // D+1 pra débito simplificado
    }
}

public sealed record SettlementItem(
    Guid Id,
    Guid TransactionId,
    decimal Amount,
    string Currency,
    string Type,
    int Installments);
