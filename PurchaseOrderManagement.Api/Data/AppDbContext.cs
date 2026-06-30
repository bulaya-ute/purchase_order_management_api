using Microsoft.EntityFrameworkCore;
using PurchaseOrderManagement.Api.Entities;
using PurchaseOrderManagement.Api.Services;

namespace PurchaseOrderManagement.Api.Data;

public class AppDbContext : DbContext
{
    private readonly ICurrentUser? _currentUser;

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUser currentUser) : base(options)
    {
        _currentUser = currentUser;
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
    public DbSet<PurchaseOrderCurrencyTotal> PurchaseOrderCurrencyTotals => Set<PurchaseOrderCurrencyTotal>();
    public DbSet<Approval> Approvals => Set<Approval>();
    public DbSet<Currency> Currencies => Set<Currency>();
    public DbSet<PurchaseOrderType> PurchaseOrderTypes => Set<PurchaseOrderType>();
    public DbSet<PurchaseOrderTypeApprovalStep> PurchaseOrderTypeApprovalSteps => Set<PurchaseOrderTypeApprovalStep>();
    public DbSet<PurchaseOrderTypeAllowedCreatorRole> PurchaseOrderTypeAllowedCreatorRoles => Set<PurchaseOrderTypeAllowedCreatorRole>();

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

        // Acting user for audit columns, from the cookie-session current-user accessor.
        // Null for system/seed operations run outside an authenticated HTTP request
        // (CreatedByUserId/UpdatedByUserId/DeletedByUserId are nullable for exactly this reason —
        // docs/05-CROSS-CUTTING-CONVENTIONS.md).
        var actingUserId = _currentUser?.UserId;

        foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAtUtc = utcNow;
                    entry.Entity.CreatedByUserId ??= actingUserId;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAtUtc = utcNow;
                    entry.Entity.UpdatedByUserId = actingUserId;
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
                entry.Entity.DeletedByUserId = actingUserId;

                if (entry.Entity is IAuditableEntity auditable)
                {
                    auditable.UpdatedAtUtc = utcNow;
                    auditable.UpdatedByUserId = actingUserId;
                }
            }
        }
    }
}
