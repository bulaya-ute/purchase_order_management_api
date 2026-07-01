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

        builder.Property(q => q.CurrencyCode)
            .HasColumnName("CurrencyCode")
            .HasColumnType("char(3)")
            .IsRequired();

        // Percentages: decimal(5,2) per docs/05-CROSS-CUTTING-CONVENTIONS.md.
        // Nullable: null = tax pre-included / no discount specified.
        builder.Property(q => q.TaxRate).HasColumnType("numeric(5,2)");
        builder.Property(q => q.DiscountRate).HasColumnType("numeric(5,2)");

        builder.HasOne(q => q.Currency)
            .WithMany()
            .HasForeignKey(q => q.CurrencyCode)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(q => q.Supplier)
            .WithMany(s => s.Quotations)
            .HasForeignKey(q => q.SupplierId)
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
