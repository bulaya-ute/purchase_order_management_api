using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PurchaseOrderManagement.Api.Entities;

namespace PurchaseOrderManagement.Api.Data.Configurations;

public class PurchaseOrderTypeConfiguration : IEntityTypeConfiguration<PurchaseOrderType>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderType> builder)
    {
        BaseEntityConfiguration.ConfigureBase(builder);

        builder.ToTable("PurchaseOrderTypes");

        builder.Property(t => t.Name).IsRequired().HasMaxLength(256);

        builder.Property(t => t.IsActive)
            .HasDefaultValue(true)
            .IsRequired();
    }
}
