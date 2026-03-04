using AcquirerFlow.Authorization.Domain.ValueObjects;

namespace AcquirerFlow.Authorization.Domain.Ports.Out;

public interface ICardTokenizer
{
    Task<CardToken> TokenizeAsync(string cardNumber, string brand);
}
