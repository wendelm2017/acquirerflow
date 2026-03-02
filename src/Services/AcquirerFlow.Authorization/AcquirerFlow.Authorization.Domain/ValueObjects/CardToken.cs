namespace AcquirerFlow.Authorization.Domain.ValueObjects;

public sealed record CardToken
{
    public string Token { get; init; } = string.Empty;
    public string LastFour { get; init; } = string.Empty;
    public string Brand { get; init; } = string.Empty;

    private CardToken() { } // EF Core

    private CardToken(string token, string lastFour, string brand)
    {
        Token = token;
        LastFour = lastFour;
        Brand = brand;
    }

    public static CardToken Create(string token, string lastFour, string brand)
    {
        if (string.IsNullOrWhiteSpace(token)) throw new ArgumentException("Token is required");
        if (string.IsNullOrWhiteSpace(lastFour) || lastFour.Length != 4 || !lastFour.All(char.IsDigit))
            throw new ArgumentException("LastFour must be exactly 4 digits");
        var validBrands = new[] { "VISA", "MASTERCARD", "ELO", "AMEX", "HIPERCARD" };
        var normalizedBrand = brand.ToUpperInvariant();
        if (!validBrands.Contains(normalizedBrand))
            throw new ArgumentException($"Invalid brand: {brand}");
        return new CardToken(token, lastFour, normalizedBrand);
    }

    public string MaskedDisplay => $"****-****-****-{LastFour}";
    public override string ToString() => $"{Brand} {MaskedDisplay}";
}
