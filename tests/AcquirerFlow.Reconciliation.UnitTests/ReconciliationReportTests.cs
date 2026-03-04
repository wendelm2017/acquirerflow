using AcquirerFlow.Reconciliation.Domain.Entities;
using AcquirerFlow.Reconciliation.Domain.ValueObjects;
using FluentAssertions;

namespace AcquirerFlow.Reconciliation.UnitTests;

public class ReconciliationReportTests
{
    private readonly DateTime _refDate = new(2026, 3, 2);

    [Fact]
    public void Create_ShouldSetDefaults()
    {
        var report = ReconciliationReport.Create(_refDate);
        report.Id.Should().NotBeEmpty();
        report.ReferenceDate.Should().Be(_refDate);
        report.Status.Should().Be("RUNNING");
        report.Entries.Should().BeEmpty();
    }

    [Fact]
    public void AddEntry_ShouldAccumulate()
    {
        var report = ReconciliationReport.Create(_refDate);
        report.AddEntry(ReconciliationEntry.Create(
            report.Id, Guid.NewGuid(), DiscrepancyType.MissingCapture, "Test"));
        report.Entries.Should().HaveCount(1);
    }

    [Fact]
    public void Complete_NoDiscrepancies_ShouldBeReconciled()
    {
        var report = ReconciliationReport.Create(_refDate);
        report.SetTotals(10, 10, 2, 5000m, 5000m, 5000m, 4875m);
        report.Complete();
        report.Status.Should().Be("RECONCILED");
        report.DiscrepancyCount.Should().Be(0);
        report.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void Complete_WithDiscrepancies_ShouldReportThem()
    {
        var report = ReconciliationReport.Create(_refDate);
        report.AddEntry(ReconciliationEntry.Create(report.Id, Guid.NewGuid(), DiscrepancyType.MissingCapture, "Missing"));
        report.AddEntry(ReconciliationEntry.Create(report.Id, Guid.NewGuid(), DiscrepancyType.AmountMismatch, "Mismatch",
            expectedAmount: 500m, actualAmount: 400m));
        report.Complete();
        report.Status.Should().Be("DISCREPANCIES_FOUND");
        report.DiscrepancyCount.Should().Be(2);
    }

    [Fact]
    public void AddEntry_ToCompletedReport_ShouldThrow()
    {
        var report = ReconciliationReport.Create(_refDate);
        report.Complete();
        var act = () => report.AddEntry(ReconciliationEntry.Create(
            report.Id, Guid.NewGuid(), DiscrepancyType.MissingCapture, "Test"));
        act.Should().Throw<InvalidOperationException>();
    }
}
