using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PurchaseOrderManagement.Api.Entities;

namespace PurchaseOrderManagement.Api.Data.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        BaseEntityConfiguration.ConfigureBase(builder);

        builder.ToTable("Roles");

        builder.Property(r => r.Name)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(r => r.IsSystemRole)
            .HasDefaultValue(false)
            .IsRequired();

        builder.HasOne(r => r.ParentRole)
            .WithMany(r => r.ChildRoles)
            .HasForeignKey(r => r.ParentRoleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
