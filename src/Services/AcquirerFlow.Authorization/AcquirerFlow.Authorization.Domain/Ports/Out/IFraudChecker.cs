namespace AcquirerFlow.Authorization.Domain.Ports.Out;

public interface IFraudChecker
{
    Task<FraudCheckResult> CheckAsync(Guid merchantId, decimal amount, string cardToken);
}

public record FraudCheckResult(bool IsApproved, string? Reason);
