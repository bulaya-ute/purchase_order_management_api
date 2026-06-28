using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PurchaseOrderManagement.Api.Entities;

namespace PurchaseOrderManagement.Api.Data.Configurations;

public class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        BaseEntityConfiguration.ConfigureBase(builder);

        builder.ToTable("Suppliers");

        builder.Property(s => s.SupplierName).IsRequired().HasMaxLength(256);
        builder.Property(s => s.Phone).IsRequired().HasMaxLength(64);
        builder.Property(s => s.Email).IsRequired().HasMaxLength(256);
        builder.Property(s => s.Address).IsRequired().HasMaxLength(512);
    }
}
