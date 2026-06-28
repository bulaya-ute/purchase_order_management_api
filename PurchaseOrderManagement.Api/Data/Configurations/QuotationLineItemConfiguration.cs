using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PurchaseOrderManagement.Api.Entities;

namespace PurchaseOrderManagement.Api.Data.Configurations;

public class QuotationLineItemConfiguration : IEntityTypeConfiguration<QuotationLineItem>
{
    public void Configure(EntityTypeBuilder<QuotationLineItem> builder)
    {
        BaseEntityConfiguration.ConfigureBase(builder);

        builder.ToTable("QuotationLineItems");

        builder.Property(li => li.Description).IsRequired().HasMaxLength(1024);
        builder.Property(li => li.Quantity).HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(li => li.UnitCost).HasColumnType("numeric(18,2)").IsRequired();

        builder.HasOne(li => li.Quotation)
            .WithMany(q => q.QuotationLineItems)
            .HasForeignKey(li => li.QuotationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
