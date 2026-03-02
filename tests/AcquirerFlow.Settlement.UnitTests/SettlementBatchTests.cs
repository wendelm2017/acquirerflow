using AcquirerFlow.Settlement.Domain.Entities;
using FluentAssertions;

namespace AcquirerFlow.Settlement.UnitTests;

public class SettlementBatchTests
{
    private readonly Guid _merchantId = Guid.Parse("550e8400-e29b-41d4-a716-446655440000");
    private readonly DateTime _refDate = new(2026, 3, 2);

    [Fact]
    public void Create_ShouldSetCorrectDefaults()
    {
        var batch = SettlementBatch.Create(_merchantId, _refDate);

        batch.MerchantId.Should().Be(_merchantId);
        batch.ReferenceDate.Should().Be(_refDate);
        batch.Status.Should().Be("PENDING");
        batch.Items.Should().BeEmpty();
    }

    [Fact]
    public void AddItem_ShouldAccumulateItems()
    {
        var batch = SettlementBatch.Create(_merchantId, _refDate);

        batch.AddItem(Guid.NewGuid(), 100m, "BRL", "CREDIT", 1);
        batch.AddItem(Guid.NewGuid(), 200m, "BRL", "CREDIT", 3);

        batch.Items.Should().HaveCount(2);
    }

    [Fact]
    public void Process_ShouldCalculateFeesAndSetStatus()
    {
        var batch = SettlementBatch.Create(_merchantId, _refDate);
        batch.AddItem(Guid.NewGuid(), 500m, "BRL", "CREDIT", 1);
        batch.AddItem(Guid.NewGuid(), 500m, "BRL", "CREDIT", 2);

        batch.Process();

        batch.Status.Should().Be("PROCESSED");
        batch.TransactionCount.Should().Be(2);
        batch.Fees.GrossAmount.Should().Be(1000m);
        batch.Fees.MdrAmount.Should().Be(25m);
        batch.Fees.NetAmount.Should().Be(975m);
        batch.SettlementDate.Should().Be(_refDate.AddDays(1)); // D+1
    }

    [Fact]
    public void Process_EmptyBatch_ShouldThrow()
    {
        var batch = SettlementBatch.Create(_merchantId, _refDate);
        var act = () => batch.Process();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Process_AlreadyProcessed_ShouldThrow()
    {
        var batch = SettlementBatch.Create(_merchantId, _refDate);
        batch.AddItem(Guid.NewGuid(), 100m, "BRL", "CREDIT", 1);
        batch.Process();

        var act = () => batch.Process();
        act.Should().Throw<InvalidOperationException>();
    }
}
