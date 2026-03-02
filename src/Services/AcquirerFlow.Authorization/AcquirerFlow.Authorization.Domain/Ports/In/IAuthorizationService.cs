using AcquirerFlow.Authorization.Domain.Entities;

namespace AcquirerFlow.Authorization.Domain.Ports.In;

public interface IAuthorizationService
{
    Task<Transaction> AuthorizeAsync(AuthorizationRequest request);
}

public record AuthorizationRequest(
    string ExternalId,
    Guid MerchantId,
    string CardNumber,
    string CardBrand,
    decimal Amount,
    string Currency,
    int Installments,
    string Type);
