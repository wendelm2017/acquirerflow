using AcquirerFlow.Authorization.Domain;
using AcquirerFlow.Authorization.Domain.ValueObjects;
using FluentAssertions;

namespace AcquirerFlow.Authorization.UnitTests.Domain.ValueObjects;

public class MoneyTests
{
    [Fact]
    public void Create_WithValidValues_ShouldSucceed()
    {
        var money = Money.Create(100.50m, "BRL");

        money.Amount.Should().Be(100.50m);
        money.Currency.Should().Be("BRL");
    }

    [Fact]
    public void Create_WithNegativeAmount_ShouldThrow()
    {
        var act = () => Money.Create(-10m);

        act.Should().Throw<DomainException>()
            .WithMessage("*negative*");
    }

    [Fact]
    public void Create_WithInvalidCurrency_ShouldThrow()
    {
        var act = () => Money.Create(100m, "ABCD");

        act.Should().Throw<DomainException>()
            .WithMessage("*3-letter*");
    }

    [Fact]
    public void Create_ShouldRoundToTwoDecimals()
    {
        var money = Money.Create(100.555m);

        money.Amount.Should().Be(100.56m);
    }

    [Fact]
    public void Add_SameCurrency_ShouldSucceed()
    {
        var a = Money.Create(100m, "BRL");
        var b = Money.Create(50m, "BRL");

        var result = a.Add(b);

        result.Amount.Should().Be(150m);
    }

    [Fact]
    public void Add_DifferentCurrency_ShouldThrow()
    {
        var brl = Money.Create(100m, "BRL");
        var usd = Money.Create(50m, "USD");

        var act = () => brl.Add(usd);

        act.Should().Throw<DomainException>()
            .WithMessage("*different currencies*");
    }

    [Fact]
    public void Subtract_ShouldReturnCorrectValue()
    {
        var a = Money.Create(100m, "BRL");
        var b = Money.Create(30m, "BRL");

        var result = a.Subtract(b);

        result.Amount.Should().Be(70m);
    }

    [Fact]
    public void Zero_ShouldReturnZeroAmount()
    {
        var zero = Money.Zero();

        zero.Amount.Should().Be(0m);
        zero.Currency.Should().Be("BRL");
    }
}
