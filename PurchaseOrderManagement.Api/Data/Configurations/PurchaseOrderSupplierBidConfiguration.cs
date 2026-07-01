using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PurchaseOrderManagement.Api.Entities;

namespace PurchaseOrderManagement.Api.Data.Configurations;

public class PurchaseOrderSupplierBidConfiguration : IEntityTypeConfiguration<PurchaseOrderSupplierBid>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderSupplierBid> builder)
    {
        builder.ToTable("PurchaseOrderSupplierBids");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.IsPrimary).IsRequired();
        builder.Property(x => x.AddedAtUtc).IsRequired();

        // Each (PO, Bid) pair can only appear once.
        builder.HasIndex(x => new { x.PurchaseOrderId, x.SupplierBidId }).IsUnique();

        builder.HasOne(x => x.PurchaseOrder)
            .WithMany(po => po.AttachedSupplierBids)
            .HasForeignKey(x => x.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.SupplierBid)
            .WithMany(sb => sb.PurchaseOrderAttachments)
            .HasForeignKey(x => x.SupplierBidId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
