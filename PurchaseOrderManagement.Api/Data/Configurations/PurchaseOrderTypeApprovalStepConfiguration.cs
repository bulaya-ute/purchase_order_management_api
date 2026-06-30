using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PurchaseOrderManagement.Api.Entities;

namespace PurchaseOrderManagement.Api.Data.Configurations;

public class PurchaseOrderTypeApprovalStepConfiguration : IEntityTypeConfiguration<PurchaseOrderTypeApprovalStep>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderTypeApprovalStep> builder)
    {
        BaseEntityConfiguration.ConfigureBase(builder);

        builder.ToTable("PurchaseOrderTypeApprovalSteps", t => t.HasCheckConstraint(
            "CK_PurchaseOrderTypeApprovalSteps_ExactlyOneRequiredRoleOrUser",
            "(\"RequiredRoleId\" IS NOT NULL AND \"RequiredUserId\" IS NULL) OR " +
            "(\"RequiredRoleId\" IS NULL AND \"RequiredUserId\" IS NOT NULL)"));

        builder.Property(s => s.SequenceOrder)
            .HasDefaultValue(0)
            .IsRequired();

        builder.HasOne(s => s.PurchaseOrderType)
            .WithMany(t => t.ApprovalSteps)
            .HasForeignKey(s => s.PurchaseOrderTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.RequiredRole)
            .WithMany()
            .HasForeignKey(s => s.RequiredRoleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.RequiredUser)
            .WithMany()
            .HasForeignKey(s => s.RequiredUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
