using AcquirerFlow.Settlement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcquirerFlow.Infrastructure.Configurations;

public class SettlementBatchConfiguration : IEntityTypeConfiguration<SettlementBatch>
{
    public void Configure(EntityTypeBuilder<SettlementBatch> builder)
    {
        builder.ToTable("SettlementBatches");
        builder.HasKey(b => b.Id);

        builder.Property(b => b.MerchantId).IsRequired();
        builder.Property(b => b.ReferenceDate);
        builder.Property(b => b.SettlementDate);
        builder.Property(b => b.TransactionCount);
        builder.Property(b => b.Status).HasMaxLength(20);
        builder.Property(b => b.CreatedAt);

        builder.OwnsOne(b => b.Fees, f =>
        {
            f.Property(x => x.GrossAmount).HasColumnName("GrossAmount").HasPrecision(18, 2);
            f.Property(x => x.MdrAmount).HasColumnName("MdrAmount").HasPrecision(18, 2);
            f.Property(x => x.InterchangeAmount).HasColumnName("InterchangeAmount").HasPrecision(18, 2);
            f.Property(x => x.SchemeFeeAmount).HasColumnName("SchemeFeeAmount").HasPrecision(18, 2);
            f.Property(x => x.AcquirerFeeAmount).HasColumnName("AcquirerFeeAmount").HasPrecision(18, 2);
            f.Property(x => x.NetAmount).HasColumnName("NetAmount").HasPrecision(18, 2);
            f.Property(x => x.MdrRate).HasColumnName("MdrRate").HasPrecision(8, 4);
            f.Property(x => x.InterchangeRate).HasColumnName("InterchangeRate").HasPrecision(8, 4);
            f.Property(x => x.SchemeFeeRate).HasColumnName("SchemeFeeRate").HasPrecision(8, 4);
        });

        builder.Ignore(b => b.Items);

        builder.HasIndex(b => b.MerchantId);
        builder.HasIndex(b => b.ReferenceDate);
    }
}
