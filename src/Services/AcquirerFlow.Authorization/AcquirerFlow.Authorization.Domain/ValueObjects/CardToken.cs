namespace AcquirerFlow.Authorization.Domain.ValueObjects;

public sealed record CardToken
{
    public string Token { get; }
    public string LastFour { get; }
    public string Brand { get; }

    private CardToken(string token, string lastFour, string brand)
    {
        Token = token;
        LastFour = lastFour;
        Brand = brand;
    }

    public static CardToken Create(string token, string lastFour, string brand)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new DomainException("Card token cannot be empty");

        if (string.IsNullOrWhiteSpace(lastFour) || lastFour.Length != 4 || !lastFour.All(char.IsDigit))
            throw new DomainException("Last four digits must be exactly 4 numeric characters");

        var validBrands = new[] { "VISA", "MASTERCARD", "ELO", "AMEX", "HIPERCARD" };
        var normalizedBrand = brand.ToUpperInvariant();

        if (!validBrands.Contains(normalizedBrand))
            throw new DomainException($"Invalid card brand: {brand}");

        return new CardToken(token, lastFour, normalizedBrand);
    }

    public string MaskedDisplay => $"****-****-****-{LastFour}";

    public override string ToString() => $"{Brand} {MaskedDisplay}";
}
