using AcquirerFlow.Authorization.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcquirerFlow.Infrastructure.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("Transactions");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.ExternalId).HasMaxLength(100).IsRequired();
        builder.Property(t => t.MerchantId).IsRequired();
        builder.Property(t => t.Type).HasMaxLength(20).IsRequired();
        builder.Property(t => t.Installments);
        builder.Property(t => t.AuthorizationCode).HasMaxLength(50);
        builder.Property(t => t.DeclinedReason).HasMaxLength(200);
        builder.Property(t => t.CreatedAt);
        builder.Property(t => t.AuthorizedAt);
        builder.Property(t => t.CapturedAt);

        builder.OwnsOne(t => t.Amount, m =>
        {
            m.Property(x => x.Amount).HasColumnName("Amount").HasPrecision(18, 2).IsRequired();
            m.Property(x => x.Currency).HasColumnName("Currency").HasMaxLength(3).IsRequired();
        });

        builder.OwnsOne(t => t.Card, c =>
        {
            c.Property(x => x.Token).HasColumnName("CardToken").HasMaxLength(200).IsRequired();
            c.Property(x => x.LastFour).HasColumnName("CardLastFour").HasMaxLength(4).IsRequired();
            c.Property(x => x.Brand).HasColumnName("CardBrand").HasMaxLength(20).IsRequired();
        });

        builder.OwnsOne(t => t.Status, s =>
        {
            s.Property(x => x.Value).HasColumnName("Status").HasMaxLength(20).IsRequired();
        });

        builder.HasIndex(t => t.ExternalId);
        builder.HasIndex(t => t.MerchantId);
    }
}
