using AcquirerFlow.Reconciliation.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcquirerFlow.Infrastructure.Configurations;

public class ReconciliationEntryConfiguration : IEntityTypeConfiguration<ReconciliationEntry>
{
    public void Configure(EntityTypeBuilder<ReconciliationEntry> builder)
    {
        builder.ToTable("ReconciliationEntries");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.ReportId);
        builder.Property(e => e.TransactionId);
        builder.Property(e => e.CaptureId);
        builder.Property(e => e.SettlementBatchId);
        builder.Property(e => e.MerchantId);
        builder.Property(e => e.Details).HasMaxLength(500);
        builder.Property(e => e.ExpectedAmount).HasPrecision(18, 2);
        builder.Property(e => e.ActualAmount).HasPrecision(18, 2);
        builder.Property(e => e.DetectedAt);

        builder.OwnsOne(e => e.Discrepancy, d =>
        {
            d.Property(x => x.Value).HasColumnName("DiscrepancyType").HasMaxLength(50).IsRequired();
        });

        builder.HasIndex(e => e.ReportId);
        builder.HasIndex(e => e.MerchantId);
    }
}
