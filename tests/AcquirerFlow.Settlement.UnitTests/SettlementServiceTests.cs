using AcquirerFlow.Contracts.Events;
using AcquirerFlow.Settlement.Application.Services;
using AcquirerFlow.Settlement.Domain.Ports.Out;
using FluentAssertions;
using Moq;

namespace AcquirerFlow.Settlement.UnitTests;

public class SettlementServiceTests
{
    private readonly Mock<ISettlementRepository> _repoMock = new();
    private readonly SettlementService _service;
    private readonly Guid _merchantId = Guid.Parse("550e8400-e29b-41d4-a716-446655440000");

    public SettlementServiceTests()
    {
        _service = new SettlementService(_repoMock.Object);
        _service.SetTestRepository(_repoMock.Object);
    }

    private TransactionCaptured CreateCapture(decimal amount = 500m, Guid? merchantId = null)
    {
        return new TransactionCaptured(
            Guid.NewGuid(), merchantId ?? _merchantId, "tok_test", "VISA",
            amount, "BRL", 1, "CREDIT", "AUTH123", DateTime.UtcNow);
    }

    [Fact]
    public async Task AccumulateCapture_ShouldStorePending()
    {
        _service.AccumulateCapture(CreateCapture());
        var batches = await _service.ProcessBatchesAsync();
        batches.Should().HaveCount(1);
    }

    [Fact]
    public async Task ProcessBatches_NoPending_ShouldReturnEmpty()
    {
        var batches = await _service.ProcessBatchesAsync();
        batches.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessBatches_MultipleMerchants_ShouldCreateSeparateBatches()
    {
        var merchant1 = Guid.NewGuid();
        var merchant2 = Guid.NewGuid();

        _service.AccumulateCapture(CreateCapture(100m, merchant1));
        _service.AccumulateCapture(CreateCapture(200m, merchant2));

        var batches = await _service.ProcessBatchesAsync();

        batches.Should().HaveCount(2);
        _repoMock.Verify(r => r.SaveAsync(It.IsAny<Domain.Entities.SettlementBatch>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ProcessBatches_ShouldClearPendingAfterProcessing()
    {
        _service.AccumulateCapture(CreateCapture());
        await _service.ProcessBatchesAsync();

        var second = await _service.ProcessBatchesAsync();
        second.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessBatches_ShouldCallSaveForEachBatch()
    {
        _service.AccumulateCapture(CreateCapture(300m));
        _service.AccumulateCapture(CreateCapture(700m));

        var batches = await _service.ProcessBatchesAsync();

        batches.Should().HaveCount(1);
        batches[0].Fees.GrossAmount.Should().Be(1000m);
        _repoMock.Verify(r => r.SaveAsync(It.IsAny<Domain.Entities.SettlementBatch>()), Times.Once);
    }
}
