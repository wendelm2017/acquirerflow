namespace AcquirerFlow.Settlement.Domain.ValueObjects;

public sealed record FeeBreakdown
{
    public decimal GrossAmount { get; init; }
    public decimal MdrRate { get; init; }
    public decimal MdrAmount { get; init; }
    public decimal InterchangeRate { get; init; }
    public decimal InterchangeAmount { get; init; }
    public decimal SchemeFeeRate { get; init; }
    public decimal SchemeFeeAmount { get; init; }
    public decimal AcquirerFeeAmount { get; init; }
    public decimal NetAmount { get; init; }

    private FeeBreakdown() { } // EF Core

    private FeeBreakdown(decimal grossAmount, decimal mdrRate, decimal interchangeRate, decimal schemeFeeRate)
    {
        GrossAmount = grossAmount;
        MdrRate = mdrRate;
        InterchangeRate = interchangeRate;
        SchemeFeeRate = schemeFeeRate;
        MdrAmount = Math.Round(grossAmount * mdrRate, 2);
        InterchangeAmount = Math.Round(grossAmount * interchangeRate, 2);
        SchemeFeeAmount = Math.Round(grossAmount * schemeFeeRate, 2);
        AcquirerFeeAmount = MdrAmount - InterchangeAmount - SchemeFeeAmount;
        NetAmount = grossAmount - MdrAmount;
    }

    public static FeeBreakdown Calculate(decimal grossAmount, decimal mdrRate = 0.025m,
        decimal interchangeRate = 0.015m, decimal schemeFeeRate = 0.003m)
    {
        if (grossAmount <= 0)
            throw new InvalidOperationException("Gross amount must be positive");
        if (mdrRate < interchangeRate + schemeFeeRate)
            throw new InvalidOperationException("MDR must be greater than interchange + scheme fee");

        return new FeeBreakdown(grossAmount, mdrRate, interchangeRate, schemeFeeRate);
    }
}
