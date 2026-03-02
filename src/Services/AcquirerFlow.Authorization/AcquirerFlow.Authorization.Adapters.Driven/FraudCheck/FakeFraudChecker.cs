using AcquirerFlow.Authorization.Domain.Ports.Out;

namespace AcquirerFlow.Authorization.Adapters.Driven.FraudCheck;

public class FakeFraudChecker : IFraudChecker
{
    // Regras simples pra simular fraude
    public Task<FraudCheckResult> CheckAsync(Guid merchantId, decimal amount, string cardToken)
    {
        // Regra 1: transações acima de R$ 10.000 são suspeitas
        if (amount > 10_000m)
            return Task.FromResult(new FraudCheckResult(false, "HIGH_VALUE_TRANSACTION"));

        // Regra 2: 10% de chance aleatória de fraude (pra simular cenário real)
        if (Random.Shared.Next(100) < 10)
            return Task.FromResult(new FraudCheckResult(false, "RANDOM_FRAUD_CHECK_FAILED"));

        return Task.FromResult(new FraudCheckResult(true, null));
    }
}
