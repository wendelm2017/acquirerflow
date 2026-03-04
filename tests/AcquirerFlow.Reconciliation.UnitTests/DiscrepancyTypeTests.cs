using AcquirerFlow.Reconciliation.Domain.ValueObjects;
using FluentAssertions;

namespace AcquirerFlow.Reconciliation.UnitTests;

public class DiscrepancyTypeTests
{
    [Fact]
    public void AllTypes_ShouldBeDistinct()
    {
        var types = new[]
        {
            DiscrepancyType.MissingCapture, DiscrepancyType.MissingSettlement,
            DiscrepancyType.AmountMismatch, DiscrepancyType.OrphanCapture,
            DiscrepancyType.OrphanSettlement, DiscrepancyType.DeclinedButCaptured
        };
        types.Select(t => t.Value).Distinct().Count().Should().Be(6);
    }

    [Fact]
    public void SameType_ShouldBeEqual()
    {
        DiscrepancyType.MissingCapture.Should().Be(DiscrepancyType.MissingCapture);
    }

    [Fact]
    public void DifferentTypes_ShouldNotBeEqual()
    {
        DiscrepancyType.MissingCapture.Should().NotBe(DiscrepancyType.AmountMismatch);
    }
}
