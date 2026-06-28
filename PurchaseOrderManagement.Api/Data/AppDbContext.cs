using Microsoft.EntityFrameworkCore;
using PurchaseOrderManagement.Api.Entities;

namespace PurchaseOrderManagement.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Company> Companies => Set<Company>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<StoredFile> Files => Set<StoredFile>();
    public DbSet<SupplierBid> SupplierBids => Set<SupplierBid>();
    public DbSet<Quotation> Quotations => Set<Quotation>();
    public DbSet<QuotationLineItem> QuotationLineItems => Set<QuotationLineItem>();
    public DbSet<SupplierBidItem> SupplierBidItems => Set<SupplierBidItem>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderLineItem> PurchaseOrderLineItems => Set<PurchaseOrderLineItem>();
    public DbSet<Approval> Approvals => Set<Approval>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Required for case-insensitive Users.Email uniqueness/lookup.
        modelBuilder.HasPostgresExtension("citext");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    public override int SaveChanges()
    {
        ApplyAuditAndSoftDeleteConventions();
        return base.SaveChanges();
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyAuditAndSoftDeleteConventions();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditAndSoftDeleteConventions();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ApplyAuditAndSoftDeleteConventions();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <summary>
    /// Stamps audit columns on add/modify and converts hard deletes into soft deletes
    /// (nothing is ever physically removed — docs/05-CROSS-CUTTING-CONVENTIONS.md).
    /// </summary>
    private void ApplyAuditAndSoftDeleteConventions()
    {
        var utcNow = DateTime.UtcNow;

        // TODO: populate CreatedByUserId/UpdatedByUserId/DeletedByUserId from the acting user
        // once authentication/current-user context exists (docs/05: cookie-based session auth,
        // not yet implemented). For now these are left as whatever the caller explicitly set.

        foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAtUtc = utcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAtUtc = utcNow;
                    break;
            }
        }

        foreach (var entry in ChangeTracker.Entries<ISoftDeletableEntity>())
        {
            if (entry.State == EntityState.Deleted)
            {
                // Convert the hard delete into a soft delete.
                entry.State = EntityState.Modified;
                entry.Entity.IsDeleted = true;
                entry.Entity.DeletedAtUtc = utcNow;

                if (entry.Entity is IAuditableEntity auditable)
                {
                    auditable.UpdatedAtUtc = utcNow;
                }
            }
        }
    }
}
