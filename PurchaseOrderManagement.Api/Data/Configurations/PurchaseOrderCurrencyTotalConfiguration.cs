using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PurchaseOrderManagement.Api.Entities;

namespace PurchaseOrderManagement.Api.Data.Configurations;

public class PurchaseOrderCurrencyTotalConfiguration : IEntityTypeConfiguration<PurchaseOrderCurrencyTotal>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderCurrencyTotal> builder)
    {
        builder.ToTable("PurchaseOrderCurrencyTotals");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.CurrencyCode)
            .HasColumnName("CurrencyCode")
            .HasColumnType("char(3)")
            .IsRequired();

        builder.Property(t => t.Subtotal).HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(t => t.TaxAmount).HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(t => t.TotalAmount).HasColumnType("numeric(18,2)").IsRequired();

        builder.HasIndex(t => new { t.PurchaseOrderId, t.CurrencyCode }).IsUnique();

        // PurchaseOrderCurrencyTotal has no soft-delete column of its own (pure computed
        // aggregate, fully recomputed/replaced whenever line items change). Its required-side
        // relationship to the soft-deletable PurchaseOrder needs a matching (no-op) query filter
        // to avoid EF's "required end filtered out" model-validation warning.
        builder.HasQueryFilter(t => !t.PurchaseOrder.IsDeleted);

        builder.HasOne(t => t.PurchaseOrder)
            .WithMany(po => po.CurrencyTotals)
            .HasForeignKey(t => t.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.Currency)
            .WithMany()
            .HasForeignKey(t => t.CurrencyCode)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
