using AcquirerFlow.Settlement.Domain.ValueObjects;
using FluentAssertions;

namespace AcquirerFlow.Settlement.UnitTests;

public class FeeBreakdownTests
{
    [Fact]
    public void Calculate_ShouldComputeCorrectFees()
    {
        var fees = FeeBreakdown.Calculate(1000m);

        fees.GrossAmount.Should().Be(1000m);
        fees.MdrAmount.Should().Be(25m);        // 2.5%
        fees.InterchangeAmount.Should().Be(15m); // 1.5%
        fees.SchemeFeeAmount.Should().Be(3m);    // 0.3%
        fees.AcquirerFeeAmount.Should().Be(7m);  // 0.7% (2.5 - 1.5 - 0.3)
        fees.NetAmount.Should().Be(975m);         // 1000 - 25
    }

    [Fact]
    public void Calculate_WithCustomRates_ShouldWork()
    {
        var fees = FeeBreakdown.Calculate(1000m, mdrRate: 0.03m, interchangeRate: 0.018m, schemeFeeRate: 0.004m);

        fees.MdrAmount.Should().Be(30m);
        fees.InterchangeAmount.Should().Be(18m);
        fees.SchemeFeeAmount.Should().Be(4m);
        fees.AcquirerFeeAmount.Should().Be(8m);
        fees.NetAmount.Should().Be(970m);
    }

    [Fact]
    public void Calculate_SmallAmount_ShouldRoundCorrectly()
    {
        var fees = FeeBreakdown.Calculate(7.99m);

        fees.MdrAmount.Should().Be(0.20m);       // 7.99 * 0.025 = 0.19975 → 0.20
        fees.NetAmount.Should().Be(7.79m);
        (fees.InterchangeAmount + fees.SchemeFeeAmount + fees.AcquirerFeeAmount).Should().Be(fees.MdrAmount);
    }

    [Fact]
    public void Calculate_ZeroAmount_ShouldThrow()
    {
        var act = () => FeeBreakdown.Calculate(0m);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Calculate_NegativeAmount_ShouldThrow()
    {
        var act = () => FeeBreakdown.Calculate(-100m);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Calculate_InvalidRates_ShouldThrow()
    {
        // interchange + scheme > mdr
        var act = () => FeeBreakdown.Calculate(1000m, mdrRate: 0.01m, interchangeRate: 0.015m, schemeFeeRate: 0.003m);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Calculate_MdrBreakdownShouldSumToMdr()
    {
        var fees = FeeBreakdown.Calculate(12345.67m);

        var sum = fees.InterchangeAmount + fees.SchemeFeeAmount + fees.AcquirerFeeAmount;
        sum.Should().Be(fees.MdrAmount);
    }
}
