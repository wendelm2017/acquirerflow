using AcquirerFlow.Capture.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcquirerFlow.Infrastructure.Configurations;

public class CapturedTransactionConfiguration : IEntityTypeConfiguration<CapturedTransaction>
{
    public void Configure(EntityTypeBuilder<CapturedTransaction> builder)
    {
        builder.ToTable("CapturedTransactions");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.OriginalTransactionId).IsRequired();
        builder.Property(t => t.MerchantId).IsRequired();
        builder.Property(t => t.CardToken).HasMaxLength(200);
        builder.Property(t => t.CardBrand).HasMaxLength(20);
        builder.Property(t => t.AuthorizedAmount).HasPrecision(18, 2);
        builder.Property(t => t.CapturedAmount).HasPrecision(18, 2);
        builder.Property(t => t.Currency).HasMaxLength(3);
        builder.Property(t => t.Installments);
        builder.Property(t => t.Type).HasMaxLength(20);
        builder.Property(t => t.AuthorizationCode).HasMaxLength(50);
        builder.Property(t => t.Status).HasMaxLength(20);
        builder.Property(t => t.AuthorizedAt);
        builder.Property(t => t.CapturedAt);

        builder.HasIndex(t => t.OriginalTransactionId).IsUnique();
        builder.HasIndex(t => t.MerchantId);
    }
}
