using AcquirerFlow.Reconciliation.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcquirerFlow.Infrastructure.Configurations;

public class ReconciliationReportConfiguration : IEntityTypeConfiguration<ReconciliationReport>
{
    public void Configure(EntityTypeBuilder<ReconciliationReport> builder)
    {
        builder.ToTable("ReconciliationReports");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.ReferenceDate);
        builder.Property(r => r.Status).HasMaxLength(30);
        builder.Property(r => r.TotalTransactions);
        builder.Property(r => r.TotalCaptures);
        builder.Property(r => r.TotalSettlements);
        builder.Property(r => r.DiscrepancyCount);
        builder.Property(r => r.TotalAuthorizedAmount).HasPrecision(18, 2);
        builder.Property(r => r.TotalCapturedAmount).HasPrecision(18, 2);
        builder.Property(r => r.TotalSettledGrossAmount).HasPrecision(18, 2);
        builder.Property(r => r.TotalSettledNetAmount).HasPrecision(18, 2);
        builder.Property(r => r.CreatedAt);
        builder.Property(r => r.CompletedAt);

        builder.HasIndex(r => r.ReferenceDate);

        builder.HasMany(r => r.Entries)
            .WithOne()
            .HasForeignKey(e => e.ReportId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(ReconciliationReport.Entries))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
