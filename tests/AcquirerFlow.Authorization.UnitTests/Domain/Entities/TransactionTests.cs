using AcquirerFlow.Authorization.Domain;
using AcquirerFlow.Authorization.Domain.Entities;
using AcquirerFlow.Authorization.Domain.ValueObjects;
using FluentAssertions;

namespace AcquirerFlow.Authorization.UnitTests.Domain.Entities;

public class TransactionTests
{
    private static CardToken ValidCard => CardToken.Create("tok_test123", "1234", "VISA");
    private static Money ValidAmount => Money.Create(150m, "BRL");

    [Fact]
    public void Create_WithValidValues_ShouldReturnPendingTransaction()
    {
        var tx = Transaction.Create("POS-001", Guid.NewGuid(), ValidCard, ValidAmount, 3, "CREDIT");

        tx.Id.Should().NotBeEmpty();
        tx.ExternalId.Should().Be("POS-001");
        tx.Status.Value.Should().Be("PENDING");
        tx.Type.Should().Be("CREDIT");
        tx.Installments.Should().Be(3);
        tx.AuthorizationCode.Should().BeNull();
    }

    [Fact]
    public void Create_DebitWithInstallments_ShouldThrow()
    {
        var act = () => Transaction.Create("POS-001", Guid.NewGuid(), ValidCard, ValidAmount, 3, "DEBIT");

        act.Should().Throw<DomainException>()
            .WithMessage("*Debit*installments*");
    }

    [Fact]
    public void Create_WithInvalidType_ShouldThrow()
    {
        var act = () => Transaction.Create("POS-001", Guid.NewGuid(), ValidCard, ValidAmount, 1, "PIX");

        act.Should().Throw<DomainException>()
            .WithMessage("*Invalid transaction type*");
    }

    [Fact]
    public void Create_WithEmptyExternalId_ShouldThrow()
    {
        var act = () => Transaction.Create("", Guid.NewGuid(), ValidCard, ValidAmount, 1, "DEBIT");

        act.Should().Throw<DomainException>()
            .WithMessage("*ExternalId*");
    }

    [Fact]
    public void Create_WithEmptyMerchantId_ShouldThrow()
    {
        var act = () => Transaction.Create("POS-001", Guid.Empty, ValidCard, ValidAmount, 1, "DEBIT");

        act.Should().Throw<DomainException>()
            .WithMessage("*MerchantId*");
    }

    [Fact]
    public void Create_WithInstallmentsOver12_ShouldThrow()
    {
        var act = () => Transaction.Create("POS-001", Guid.NewGuid(), ValidCard, ValidAmount, 13, "CREDIT");

        act.Should().Throw<DomainException>()
            .WithMessage("*between 1 and 12*");
    }

    [Fact]
    public void Authorize_ShouldSetStatusAndCode()
    {
        var tx = Transaction.Create("POS-001", Guid.NewGuid(), ValidCard, ValidAmount, 1, "DEBIT");

        tx.Authorize("123456");

        tx.Status.Value.Should().Be("AUTHORIZED");
        tx.AuthorizationCode.Should().Be("123456");
        tx.AuthorizedAt.Should().NotBeNull();
    }

    [Fact]
    public void Authorize_WithEmptyCode_ShouldThrow()
    {
        var tx = Transaction.Create("POS-001", Guid.NewGuid(), ValidCard, ValidAmount, 1, "DEBIT");

        var act = () => tx.Authorize("");

        act.Should().Throw<DomainException>()
            .WithMessage("*Authorization code*");
    }

    [Fact]
    public void Decline_ShouldSetStatusAndReason()
    {
        var tx = Transaction.Create("POS-001", Guid.NewGuid(), ValidCard, ValidAmount, 1, "DEBIT");

        tx.Decline("INSUFFICIENT_FUNDS");

        tx.Status.Value.Should().Be("DECLINED");
        tx.DeclinedReason.Should().Be("INSUFFICIENT_FUNDS");
    }

    [Fact]
    public void Capture_AfterAuthorize_ShouldSucceed()
    {
        var tx = Transaction.Create("POS-001", Guid.NewGuid(), ValidCard, ValidAmount, 1, "DEBIT");
        tx.Authorize("123456");

        tx.Capture();

        tx.Status.Value.Should().Be("CAPTURED");
        tx.CapturedAt.Should().NotBeNull();
    }

    [Fact]
    public void Capture_WithoutAuthorize_ShouldThrow()
    {
        var tx = Transaction.Create("POS-001", Guid.NewGuid(), ValidCard, ValidAmount, 1, "DEBIT");

        var act = () => tx.Capture();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot capture*PENDING*");
    }
}
