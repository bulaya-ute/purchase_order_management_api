using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PurchaseOrderManagement.Api.Entities;

namespace PurchaseOrderManagement.Api.Data.Configurations;

public class QuotationConfiguration : IEntityTypeConfiguration<Quotation>
{
    public void Configure(EntityTypeBuilder<Quotation> builder)
    {
        BaseEntityConfiguration.ConfigureBase(builder);

        builder.ToTable("Quotations");

        builder.Property(q => q.QuoteReference).HasMaxLength(128);
        builder.Property(q => q.QuoteDate).IsRequired();
        builder.Property(q => q.ExpiresAtUtc);
        builder.Property(q => q.Notes).HasMaxLength(2048);

        builder.HasOne(q => q.SupplierBid)
            .WithMany(sb => sb.Quotations)
            .HasForeignKey(q => q.SupplierBidId)
            .OnDelete(DeleteBehavior.Restrict);

        // FileId is mandatory — every quotation must have an uploaded file.
        builder.Property(q => q.FileId).IsRequired();

        builder.HasOne(q => q.File)
            .WithMany(f => f.Quotations)
            .HasForeignKey(q => q.FileId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
    }
}
