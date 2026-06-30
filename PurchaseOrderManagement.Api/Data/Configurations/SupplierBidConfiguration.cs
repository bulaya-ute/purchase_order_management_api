using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PurchaseOrderManagement.Api.Entities;

namespace PurchaseOrderManagement.Api.Data.Configurations;

public class SupplierBidConfiguration : IEntityTypeConfiguration<SupplierBid>
{
    public void Configure(EntityTypeBuilder<SupplierBid> builder)
    {
        BaseEntityConfiguration.ConfigureBase(builder);

        builder.ToTable("SupplierBids");

        // Financially sensitive / commonly edited concurrently — docs/05-CROSS-CUTTING-CONVENTIONS.md.
        BaseEntityConfiguration.ConfigureXminConcurrency(builder);

        builder.Property(sb => sb.Notes).HasMaxLength(2048);

        // PurchaseOrderId is now nullable — a bid can be a standalone library record.
        builder.HasOne(sb => sb.PurchaseOrder)
            .WithMany(po => po.SupplierBids)
            .HasForeignKey(sb => sb.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(sb => sb.Supplier)
            .WithMany(s => s.SupplierBids)
            .HasForeignKey(sb => sb.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
