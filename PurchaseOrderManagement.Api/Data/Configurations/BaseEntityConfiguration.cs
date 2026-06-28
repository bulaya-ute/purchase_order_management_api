using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PurchaseOrderManagement.Api.Entities;

namespace PurchaseOrderManagement.Api.Data.Configurations;

/// <summary>
/// Shared configuration for the cross-cutting columns every entity carries: int identity PK,
/// audit columns (CreatedAtUtc/By, UpdatedAtUtc/By), and soft-delete columns
/// (IsDeleted, DeletedAtUtc, DeletedByUserId). See docs/05-CROSS-CUTTING-CONVENTIONS.md.
/// Concrete <see cref="IEntityTypeConfiguration{T}"/> classes should call
/// <see cref="ConfigureBase{T}"/> before configuring entity-specific columns/relationships.
/// </summary>
public static class BaseEntityConfiguration
{
    public static void ConfigureBase<T>(EntityTypeBuilder<T> builder) where T : BaseEntity
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.CreatedAtUtc).IsRequired();
        builder.Property(e => e.CreatedByUserId);
        builder.Property(e => e.UpdatedAtUtc);
        builder.Property(e => e.UpdatedByUserId);

        builder.Property(e => e.IsDeleted).HasDefaultValue(false).IsRequired();
        builder.Property(e => e.DeletedAtUtc);
        builder.Property(e => e.DeletedByUserId);

        // Global soft-delete filter — excludes IsDeleted rows everywhere by default.
        builder.HasQueryFilter(e => !e.IsDeleted);

        // CreatedByUserId / UpdatedByUserId / DeletedByUserId -> Users FKs.
        // Restrict avoids cascade-delete cycles through the Users table; soft delete makes
        // cascade behavior largely moot in practice, but Restrict is the safe default.
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.UpdatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.DeletedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    /// <summary>
    /// Configures optimistic concurrency using PostgreSQL's built-in <c>xmin</c> system column
    /// as the concurrency token (docs/05-CROSS-CUTTING-CONVENTIONS.md). This is the non-obsolete
    /// equivalent of <c>UseXminAsConcurrencyToken()</c>: a shadow <see cref="uint"/> property
    /// mapped to the "xmin" column ("xid" type), value-generated on add and update, marked as the
    /// concurrency token. Applied only to financially sensitive / concurrently edited tables.
    /// </summary>
    public static void ConfigureXminConcurrency<T>(EntityTypeBuilder<T> builder) where T : BaseEntity
    {
        builder.Property<uint>("xmin")
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();
    }
}
