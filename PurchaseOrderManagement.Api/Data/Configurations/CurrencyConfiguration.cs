using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PurchaseOrderManagement.Api.Entities;

namespace PurchaseOrderManagement.Api.Data.Configurations;

public class CurrencyConfiguration : IEntityTypeConfiguration<Currency>
{
    public void Configure(EntityTypeBuilder<Currency> builder)
    {
        builder.ToTable("Currencies");

        builder.HasKey(c => c.Code);

        builder.Property(c => c.Code)
            .HasColumnType("char(3)")
            .IsRequired();

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(c => c.IsActive)
            .HasDefaultValue(true)
            .IsRequired();
    }
}
