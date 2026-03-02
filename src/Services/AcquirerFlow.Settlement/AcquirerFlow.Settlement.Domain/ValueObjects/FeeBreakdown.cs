namespace AcquirerFlow.Settlement.Domain.ValueObjects;

public sealed record FeeBreakdown
{
    public decimal GrossAmount { get; }
    public decimal MdrRate { get; }
    public decimal MdrAmount { get; }
    public decimal InterchangeRate { get; }
    public decimal InterchangeAmount { get; }
    public decimal SchemeFeeRate { get; }
    public decimal SchemeFeeAmount { get; }
    public decimal AcquirerFeeAmount { get; }
    public decimal NetAmount { get; }

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
