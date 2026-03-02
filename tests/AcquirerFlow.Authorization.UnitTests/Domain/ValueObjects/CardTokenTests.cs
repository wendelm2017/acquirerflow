using AcquirerFlow.Authorization.Domain;
using AcquirerFlow.Authorization.Domain.ValueObjects;
using FluentAssertions;

namespace AcquirerFlow.Authorization.UnitTests.Domain.ValueObjects;

public class CardTokenTests
{
    [Fact]
    public void Create_WithValidValues_ShouldSucceed()
    {
        var token = CardToken.Create("tok_abc123", "1234", "VISA");

        token.Token.Should().Be("tok_abc123");
        token.LastFour.Should().Be("1234");
        token.Brand.Should().Be("VISA");
    }

    [Fact]
    public void Create_ShouldNormalizeBrandToUpper()
    {
        var token = CardToken.Create("tok_abc", "1234", "visa");

        token.Brand.Should().Be("VISA");
    }

    [Fact]
    public void Create_WithInvalidBrand_ShouldThrow()
    {
        var act = () => CardToken.Create("tok_abc", "1234", "DINERS");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Invalid brand*");
    }

    [Fact]
    public void Create_WithInvalidLastFour_ShouldThrow()
    {
        var act = () => CardToken.Create("tok_abc", "12", "VISA");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*4 digits*");
    }

    [Fact]
    public void Create_WithNonNumericLastFour_ShouldThrow()
    {
        var act = () => CardToken.Create("tok_abc", "ABCD", "VISA");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*4 digits*");
    }

    [Fact]
    public void Create_WithEmptyToken_ShouldThrow()
    {
        var act = () => CardToken.Create("", "1234", "VISA");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*required*");
    }

    [Fact]
    public void MaskedDisplay_ShouldMaskCorrectly()
    {
        var token = CardToken.Create("tok_abc", "1234", "VISA");

        token.MaskedDisplay.Should().Be("****-****-****-1234");
    }
}
