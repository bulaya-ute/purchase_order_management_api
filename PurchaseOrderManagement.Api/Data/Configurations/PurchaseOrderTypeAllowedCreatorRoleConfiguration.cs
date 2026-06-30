using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PurchaseOrderManagement.Api.Entities;

namespace PurchaseOrderManagement.Api.Data.Configurations;

public class PurchaseOrderTypeAllowedCreatorRoleConfiguration : IEntityTypeConfiguration<PurchaseOrderTypeAllowedCreatorRole>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderTypeAllowedCreatorRole> builder)
    {
        BaseEntityConfiguration.ConfigureBase(builder);

        builder.ToTable("PurchaseOrderTypeAllowedCreatorRoles");

        builder.HasIndex(r => new { r.PurchaseOrderTypeId, r.RoleId }).IsUnique();

        builder.HasOne(r => r.PurchaseOrderType)
            .WithMany(t => t.AllowedCreatorRoles)
            .HasForeignKey(r => r.PurchaseOrderTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Role)
            .WithMany()
            .HasForeignKey(r => r.RoleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
