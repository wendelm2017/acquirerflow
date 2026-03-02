using AcquirerFlow.Authorization.Domain.Ports.Out;
using AcquirerFlow.Authorization.Domain.ValueObjects;

namespace AcquirerFlow.Authorization.Adapters.Driven.TokenService;

public class FakeCardTokenizer : ICardTokenizer
{
    public Task<CardToken> TokenizeAsync(string cardNumber, string brand)
    {
        // PCI DSS: nunca loga o PAN completo
        var lastFour = cardNumber.Length >= 4
            ? cardNumber[^4..]
            : cardNumber.PadLeft(4, '0');

        var token = $"tok_{Guid.NewGuid():N}";

        return Task.FromResult(CardToken.Create(token, lastFour, brand));
    }
}
