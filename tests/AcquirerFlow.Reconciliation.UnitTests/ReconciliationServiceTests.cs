using AcquirerFlow.Reconciliation.Application.Services;
using AcquirerFlow.Reconciliation.Domain.Entities;
using AcquirerFlow.Reconciliation.Domain.Ports.Out;
using FluentAssertions;
using Moq;

namespace AcquirerFlow.Reconciliation.UnitTests;

public class ReconciliationServiceTests
{
    private readonly Mock<IReconciliationRepository> _repoMock = new();
    private readonly Mock<ITransactionDataSource> _dataSourceMock = new();
    private readonly ReconciliationService _service;
    private readonly DateTime _refDate = new(2026, 3, 2);
    private readonly Guid _merchant1 = Guid.Parse("550e8400-e29b-41d4-a716-446655440000");

    public ReconciliationServiceTests()
    {
        _service = new ReconciliationService(_repoMock.Object, _dataSourceMock.Object);
    }

    private void Setup(TransactionSnapshot[]? txs = null, CaptureSnapshot[]? caps = null, SettlementSnapshot[]? stls = null)
    {
        _dataSourceMock.Setup(d => d.GetAuthorizedTransactionsAsync(It.IsAny<DateTime>()))
            .ReturnsAsync((txs ?? Array.Empty<TransactionSnapshot>()).ToList());
        _dataSourceMock.Setup(d => d.GetCapturesAsync(It.IsAny<DateTime>()))
            .ReturnsAsync((caps ?? Array.Empty<CaptureSnapshot>()).ToList());
        _dataSourceMock.Setup(d => d.GetSettlementBatchesAsync(It.IsAny<DateTime>()))
            .ReturnsAsync((stls ?? Array.Empty<SettlementSnapshot>()).ToList());
    }

    [Fact]
    public async Task Run_AllReconciled_ShouldReturnZeroDiscrepancies()
    {
        var txId = Guid.NewGuid();
        Setup(
            txs: new[] { new TransactionSnapshot(txId, _merchant1, "AUTHORIZED", 1000m, "BRL", _refDate) },
            caps: new[] { new CaptureSnapshot(Guid.NewGuid(), txId, _merchant1, 1000m, "BRL", _refDate) },
            stls: new[] { new SettlementSnapshot(Guid.NewGuid(), _merchant1, 1, 1000m, 975m, "PROCESSED", _refDate) });

        var report = await _service.RunReconciliationAsync(_refDate);
        report.Status.Should().Be("RECONCILED");
        report.DiscrepancyCount.Should().Be(0);
        _repoMock.Verify(r => r.SaveAsync(It.IsAny<ReconciliationReport>()), Times.Once);
    }

    [Fact]
    public async Task Run_MissingCapture_ShouldDetect()
    {
        var txId = Guid.NewGuid();
        Setup(txs: new[] { new TransactionSnapshot(txId, _merchant1, "AUTHORIZED", 500m, "BRL", _refDate) });

        var report = await _service.RunReconciliationAsync(_refDate);
        report.Status.Should().Be("DISCREPANCIES_FOUND");
        report.Entries.Should().ContainSingle(e => e.Discrepancy == "MISSING_CAPTURE");
    }

    [Fact]
    public async Task Run_AmountMismatch_ShouldDetect()
    {
        var txId = Guid.NewGuid();
        Setup(
            txs: new[] { new TransactionSnapshot(txId, _merchant1, "AUTHORIZED", 1000m, "BRL", _refDate) },
            caps: new[] { new CaptureSnapshot(Guid.NewGuid(), txId, _merchant1, 900m, "BRL", _refDate) },
            stls: new[] { new SettlementSnapshot(Guid.NewGuid(), _merchant1, 1, 900m, 877.5m, "PROCESSED", _refDate) });

        var report = await _service.RunReconciliationAsync(_refDate);
        report.Entries.Should().Contain(e => e.Discrepancy == "AMOUNT_MISMATCH");
        var entry = report.Entries.First(e => e.Discrepancy == "AMOUNT_MISMATCH");
        entry.ExpectedAmount.Should().Be(1000m);
        entry.ActualAmount.Should().Be(900m);
    }

    [Fact]
    public async Task Run_OrphanCapture_ShouldDetect()
    {
        Setup(caps: new[] { new CaptureSnapshot(Guid.NewGuid(), Guid.NewGuid(), _merchant1, 500m, "BRL", _refDate) });

        var report = await _service.RunReconciliationAsync(_refDate);
        report.Entries.Should().Contain(e => e.Discrepancy == "ORPHAN_CAPTURE");
    }

    [Fact]
    public async Task Run_DeclinedButCaptured_ShouldDetect()
    {
        var txId = Guid.NewGuid();
        Setup(
            txs: new[] { new TransactionSnapshot(txId, _merchant1, "DECLINED", 500m, "BRL", _refDate) },
            caps: new[] { new CaptureSnapshot(Guid.NewGuid(), txId, _merchant1, 500m, "BRL", _refDate) });

        var report = await _service.RunReconciliationAsync(_refDate);
        report.Entries.Should().Contain(e => e.Discrepancy == "DECLINED_BUT_CAPTURED");
    }

    [Fact]
    public async Task Run_SettlementMismatch_ShouldDetect()
    {
        var tx1 = Guid.NewGuid();
        var tx2 = Guid.NewGuid();
        Setup(
            txs: new[]
            {
                new TransactionSnapshot(tx1, _merchant1, "AUTHORIZED", 1000m, "BRL", _refDate),
                new TransactionSnapshot(tx2, _merchant1, "AUTHORIZED", 2000m, "BRL", _refDate)
            },
            caps: new[]
            {
                new CaptureSnapshot(Guid.NewGuid(), tx1, _merchant1, 1000m, "BRL", _refDate),
                new CaptureSnapshot(Guid.NewGuid(), tx2, _merchant1, 2000m, "BRL", _refDate)
            },
            stls: new[] { new SettlementSnapshot(Guid.NewGuid(), _merchant1, 2, 2500m, 2437.5m, "PROCESSED", _refDate) });

        var report = await _service.RunReconciliationAsync(_refDate);
        report.Entries.Should().Contain(e => e.Discrepancy == "MISSING_SETTLEMENT");
        var entry = report.Entries.First(e => e.Discrepancy == "MISSING_SETTLEMENT");
        entry.ExpectedAmount.Should().Be(3000m);
        entry.ActualAmount.Should().Be(2500m);
    }

    [Fact]
    public async Task Run_MultipleDiscrepancies_ShouldDetectAll()
    {
        var tx1 = Guid.NewGuid();
        var tx2 = Guid.NewGuid();
        var tx3 = Guid.NewGuid();
        Setup(
            txs: new[]
            {
                new TransactionSnapshot(tx1, _merchant1, "AUTHORIZED", 1000m, "BRL", _refDate),
                new TransactionSnapshot(tx2, _merchant1, "AUTHORIZED", 500m, "BRL", _refDate),
                new TransactionSnapshot(tx3, _merchant1, "DECLINED", 300m, "BRL", _refDate)
            },
            caps: new[]
            {
                new CaptureSnapshot(Guid.NewGuid(), tx2, _merchant1, 400m, "BRL", _refDate),
                new CaptureSnapshot(Guid.NewGuid(), tx3, _merchant1, 300m, "BRL", _refDate),
                new CaptureSnapshot(Guid.NewGuid(), Guid.NewGuid(), _merchant1, 200m, "BRL", _refDate)
            });

        var report = await _service.RunReconciliationAsync(_refDate);
        report.DiscrepancyCount.Should().BeGreaterThanOrEqualTo(4);
        report.Entries.Should().Contain(e => e.Discrepancy == "MISSING_CAPTURE");
        report.Entries.Should().Contain(e => e.Discrepancy == "AMOUNT_MISMATCH");
        report.Entries.Should().Contain(e => e.Discrepancy == "ORPHAN_CAPTURE");
        report.Entries.Should().Contain(e => e.Discrepancy == "DECLINED_BUT_CAPTURED");
    }

    [Fact]
    public async Task Run_EmptyDatabase_ShouldReturnReconciled()
    {
        Setup();
        var report = await _service.RunReconciliationAsync(_refDate);
        report.Status.Should().Be("RECONCILED");
        report.DiscrepancyCount.Should().Be(0);
    }

    [Fact]
    public async Task Run_Totals_ShouldBeCorrect()
    {
        var tx1 = Guid.NewGuid();
        var tx2 = Guid.NewGuid();
        Setup(
            txs: new[]
            {
                new TransactionSnapshot(tx1, _merchant1, "AUTHORIZED", 1000m, "BRL", _refDate),
                new TransactionSnapshot(tx2, _merchant1, "AUTHORIZED", 2000m, "BRL", _refDate)
            },
            caps: new[]
            {
                new CaptureSnapshot(Guid.NewGuid(), tx1, _merchant1, 1000m, "BRL", _refDate),
                new CaptureSnapshot(Guid.NewGuid(), tx2, _merchant1, 2000m, "BRL", _refDate)
            },
            stls: new[] { new SettlementSnapshot(Guid.NewGuid(), _merchant1, 2, 3000m, 2925m, "PROCESSED", _refDate) });

        var report = await _service.RunReconciliationAsync(_refDate);
        report.TotalAuthorizedAmount.Should().Be(3000m);
        report.TotalCapturedAmount.Should().Be(3000m);
        report.TotalSettledGrossAmount.Should().Be(3000m);
        report.TotalSettledNetAmount.Should().Be(2925m);
    }
}
