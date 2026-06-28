using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PurchaseOrderManagement.Api.Entities;

namespace PurchaseOrderManagement.Api.Data.Configurations;

public class PurchaseOrderConfiguration : IEntityTypeConfiguration<PurchaseOrder>
{
    public void Configure(EntityTypeBuilder<PurchaseOrder> builder)
    {
        BaseEntityConfiguration.ConfigureBase(builder);

        builder.ToTable("PurchaseOrders");

        // Financially sensitive / commonly edited concurrently — docs/05-CROSS-CUTTING-CONVENTIONS.md.
        BaseEntityConfiguration.ConfigureXminConcurrency(builder);

        builder.Property(po => po.PONumber).IsRequired().HasMaxLength(32);
        builder.HasIndex(po => po.PONumber).IsUnique();

        builder.Property(po => po.Currency)
            .IsRequired()
            .HasColumnType("char(3)");

        builder.Property(po => po.Status)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(po => po.AwardedAtUtc);
        builder.Property(po => po.PaidAtUtc);
        builder.Property(po => po.DeliveredAtUtc);

        builder.Property(po => po.Subtotal).HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(po => po.TaxAmount).HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(po => po.TotalAmount).HasColumnType("numeric(18,2)").IsRequired();

        builder.Property(po => po.Notes).HasMaxLength(2048);

        builder.HasOne(po => po.Company)
            .WithMany(c => c.PurchaseOrders)
            .HasForeignKey(po => po.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(po => po.IssuerUser)
            .WithMany()
            .HasForeignKey(po => po.IssuerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(po => po.AwardedByUser)
            .WithMany()
            .HasForeignKey(po => po.AwardedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // AwardedSupplierBidId <-> SupplierBids.PurchaseOrderId is a circular FK pair.
        // Keep AwardedSupplierBidId nullable and Restrict on delete on both sides to avoid cycles.
        // Insert order in practice: create PO -> create bids -> set the award.
        builder.HasOne(po => po.AwardedSupplierBid)
            .WithMany(sb => sb.AwardedByPurchaseOrders)
            .HasForeignKey(po => po.AwardedSupplierBidId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
