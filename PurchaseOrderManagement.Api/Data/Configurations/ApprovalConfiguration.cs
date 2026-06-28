using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PurchaseOrderManagement.Api.Entities;

namespace PurchaseOrderManagement.Api.Data.Configurations;

public class ApprovalConfiguration : IEntityTypeConfiguration<Approval>
{
    public void Configure(EntityTypeBuilder<Approval> builder)
    {
        BaseEntityConfiguration.ConfigureBase(builder);

        builder.ToTable("Approvals", t => t.HasCheckConstraint(
            "CK_Approvals_ExactlyOneRequiredRoleOrUser",
            "(\"RequiredRoleId\" IS NOT NULL AND \"RequiredUserId\" IS NULL) OR " +
            "(\"RequiredRoleId\" IS NULL AND \"RequiredUserId\" IS NOT NULL)"));

        // Financially sensitive / commonly edited concurrently — docs/05-CROSS-CUTTING-CONVENTIONS.md.
        BaseEntityConfiguration.ConfigureXminConcurrency(builder);

        builder.Property(a => a.SequenceOrder)
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(a => a.Status)
            .HasConversion<string>()
            .HasMaxLength(16)
            .HasDefaultValue(Enums.ApprovalStatus.Pending)
            .IsRequired();

        builder.Property(a => a.ApprovedAtUtc);
        builder.Property(a => a.Comment).HasMaxLength(2048);

        builder.HasOne(a => a.PurchaseOrder)
            .WithMany(po => po.Approvals)
            .HasForeignKey(a => a.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.RequiredRole)
            .WithMany(r => r.RequiredForApprovals)
            .HasForeignKey(a => a.RequiredRoleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.RequiredUser)
            .WithMany()
            .HasForeignKey(a => a.RequiredUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.ApprovedByUser)
            .WithMany()
            .HasForeignKey(a => a.ApprovedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
