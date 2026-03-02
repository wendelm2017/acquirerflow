namespace AcquirerFlow.Authorization.Application.DTOs;

public record AuthorizationResponseDto(
    Guid TransactionId,
    string Status,
    string? AuthorizationCode,
    string? DeclinedReason,
    string CardMasked,
    decimal Amount,
    string Currency,
    DateTime ProcessedAt);
