using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PurchaseOrderManagement.Api.Entities;

namespace PurchaseOrderManagement.Api.Data.Configurations;

public class SupplierBidItemConfiguration : IEntityTypeConfiguration<SupplierBidItem>
{
    public void Configure(EntityTypeBuilder<SupplierBidItem> builder)
    {
        BaseEntityConfiguration.ConfigureBase(builder);

        builder.ToTable("SupplierBidItems");

        // Financially sensitive / commonly edited concurrently — docs/05-CROSS-CUTTING-CONVENTIONS.md.
        BaseEntityConfiguration.ConfigureXminConcurrency(builder);

        builder.Property(bi => bi.Description).IsRequired().HasMaxLength(1024);
        builder.Property(bi => bi.Quantity).HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(bi => bi.UnitCost).HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(bi => bi.DiscountPercentage).HasColumnType("numeric(5,2)");
        builder.Property(bi => bi.DiscountAmount).HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(bi => bi.TaxPercentage).HasColumnType("numeric(5,2)");
        builder.Property(bi => bi.TaxAmount).HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(bi => bi.LineSubtotal).HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(bi => bi.LineTotal).HasColumnType("numeric(18,2)").IsRequired();

        builder.Property(bi => bi.CurrencyCode)
            .HasColumnName("CurrencyCode")
            .HasColumnType("char(3)")
            .IsRequired();

        builder.HasOne(bi => bi.Currency)
            .WithMany()
            .HasForeignKey(bi => bi.CurrencyCode)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(bi => bi.SupplierBid)
            .WithMany(sb => sb.SupplierBidItems)
            .HasForeignKey(bi => bi.SupplierBidId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(bi => bi.SourceQuotationLineItem)
            .WithMany(li => li.SupplierBidItems)
            .HasForeignKey(bi => bi.SourceQuotationLineItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
