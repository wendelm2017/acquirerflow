namespace AcquirerFlow.Authorization.Application.DTOs;

public record AuthorizationRequestDto(
    string ExternalId,
    Guid MerchantId,
    string CardNumber,
    string CardBrand,
    decimal Amount,
    string Currency,
    int Installments,
    string Type);
