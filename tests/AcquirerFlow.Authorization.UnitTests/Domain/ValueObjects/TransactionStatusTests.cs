using AcquirerFlow.Authorization.Domain;
using AcquirerFlow.Authorization.Domain.ValueObjects;
using FluentAssertions;

namespace AcquirerFlow.Authorization.UnitTests.Domain.ValueObjects;

public class TransactionStatusTests
{
    [Fact]
    public void Pending_Authorize_ShouldReturnAuthorized()
    {
        var status = TransactionStatus.Pending;

        var result = status.Authorize();

        result.Value.Should().Be("AUTHORIZED");
    }

    [Fact]
    public void Pending_Decline_ShouldReturnDeclined()
    {
        var status = TransactionStatus.Pending;

        var result = status.Decline();

        result.Value.Should().Be("DECLINED");
    }

    [Fact]
    public void Authorized_Capture_ShouldReturnCaptured()
    {
        var status = TransactionStatus.Authorized;

        var result = status.Capture();

        result.Value.Should().Be("CAPTURED");
    }

    [Fact]
    public void Authorized_Authorize_ShouldThrow()
    {
        var status = TransactionStatus.Authorized;

        var act = () => status.Authorize();

        act.Should().Throw<DomainException>()
            .WithMessage("*Cannot authorize*AUTHORIZED*");
    }

    [Fact]
    public void Declined_Capture_ShouldThrow()
    {
        var status = TransactionStatus.Declined;

        var act = () => status.Capture();

        act.Should().Throw<DomainException>()
            .WithMessage("*Cannot capture*DECLINED*");
    }
}
