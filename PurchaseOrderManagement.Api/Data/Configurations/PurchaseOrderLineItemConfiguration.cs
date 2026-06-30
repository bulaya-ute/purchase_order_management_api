using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PurchaseOrderManagement.Api.Entities;

namespace PurchaseOrderManagement.Api.Data.Configurations;

public class PurchaseOrderLineItemConfiguration : IEntityTypeConfiguration<PurchaseOrderLineItem>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderLineItem> builder)
    {
        BaseEntityConfiguration.ConfigureBase(builder);

        builder.ToTable("PurchaseOrderLineItems");

        // Financially sensitive / commonly edited concurrently — docs/05-CROSS-CUTTING-CONVENTIONS.md.
        BaseEntityConfiguration.ConfigureXminConcurrency(builder);

        builder.Property(li => li.Description).IsRequired().HasMaxLength(1024);
        builder.Property(li => li.Quantity).HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(li => li.UnitCost).HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(li => li.DiscountPercentage).HasColumnType("numeric(5,2)");
        builder.Property(li => li.DiscountAmount).HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(li => li.TaxPercentage).HasColumnType("numeric(5,2)");
        builder.Property(li => li.TaxAmount).HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(li => li.LineSubtotal).HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(li => li.LineTotal).HasColumnType("numeric(18,2)").IsRequired();

        builder.Property(li => li.CurrencyCode)
            .HasColumnName("CurrencyCode")
            .HasColumnType("char(3)")
            .IsRequired();

        builder.HasOne(li => li.Currency)
            .WithMany()
            .HasForeignKey(li => li.CurrencyCode)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(li => li.PurchaseOrder)
            .WithMany(po => po.PurchaseOrderLineItems)
            .HasForeignKey(li => li.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(li => li.SourceSupplierBidItem)
            .WithMany(bi => bi.PurchaseOrderLineItems)
            .HasForeignKey(li => li.SourceSupplierBidItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
