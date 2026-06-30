using Microsoft.EntityFrameworkCore;
using PurchaseOrderManagement.Api.Data;
using PurchaseOrderManagement.Api.Entities;
using PurchaseOrderManagement.Api.Services;

namespace PurchaseOrderManagement.Api.Seeding;

/// <summary>
/// Idempotent startup seeding: a root "Head Office" company, the root "Super Admin" role
/// (docs/01-IDENTITY-AND-ROLES.md — role tree root, ParentRoleId null, IsSystemRole true), and a
/// Super Admin user assigned that role. Safe to run repeatedly — every step checks for an
/// existing row before inserting. Runs with no acting user (system operation), which is why
/// CreatedByUserId is left null on seeded rows (docs/05-CROSS-CUTTING-CONVENTIONS.md).
/// </summary>
public static class DataSeeder
{
    private const string RootCompanyName = "Head Office";
    private const string SuperAdminRoleName = "Super Admin";
    private const string DefaultAdminEmail = "admin@local";
    private const string DefaultAdminPasswordFallback = "ChangeMe!123";
    private const string DefaultCurrencyCode = "ZMW";
    private const string DefaultCurrencyName = "Zambian Kwacha";

    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DataSeeder).FullName!);

        var zmw = await db.Currencies.FirstOrDefaultAsync(c => c.Code == DefaultCurrencyCode, cancellationToken);
        if (zmw is null)
        {
            zmw = new Currency { Code = DefaultCurrencyCode, Name = DefaultCurrencyName, IsActive = true };
            db.Currencies.Add(zmw);
            await db.SaveChangesAsync(cancellationToken);
        }

        var company = await db.Companies.FirstOrDefaultAsync(c => c.ParentCompanyId == null, cancellationToken);
        if (company is null)
        {
            company = new Company { Name = RootCompanyName, ParentCompanyId = null };
            db.Companies.Add(company);
            await db.SaveChangesAsync(cancellationToken);
        }

        var superAdminRole = await db.Roles.FirstOrDefaultAsync(r => r.ParentRoleId == null, cancellationToken);
        if (superAdminRole is null)
        {
            superAdminRole = new Role
            {
                Name = SuperAdminRoleName,
                ParentRoleId = null,
                IsSystemRole = true,
            };
            db.Roles.Add(superAdminRole);
            await db.SaveChangesAsync(cancellationToken);
        }

        var adminEmail = configuration["Seed:AdminEmail"] ?? DefaultAdminEmail;
        var adminPassword = configuration["Seed:AdminPassword"];
        if (string.IsNullOrEmpty(adminPassword))
        {
            adminPassword = DefaultAdminPasswordFallback;
            logger.LogWarning(
                "Seed:AdminPassword is not configured; falling back to the development default password. " +
                "Set Seed:AdminPassword explicitly for any non-local environment.");
        }

        var adminUser = await db.Users.FirstOrDefaultAsync(u => u.Email == adminEmail, cancellationToken);
        if (adminUser is null)
        {
            var (hash, salt) = passwordHasher.Create(adminPassword);
            adminUser = new User
            {
                FullName = SuperAdminRoleName,
                Email = adminEmail,
                PasswordHash = hash,
                PasswordSalt = salt,
                IsActive = true,
                CompanyId = company.Id,
            };
            db.Users.Add(adminUser);
            await db.SaveChangesAsync(cancellationToken);
        }

        var hasSuperAdminRole = await db.UserRoles
            .AnyAsync(ur => ur.UserId == adminUser.Id && ur.RoleId == superAdminRole.Id, cancellationToken);
        if (!hasSuperAdminRole)
        {
            db.UserRoles.Add(new UserRole { UserId = adminUser.Id, RoleId = superAdminRole.Id });
            await db.SaveChangesAsync(cancellationToken);
        }

        Console.WriteLine($"[Seed] Super Admin user ready: {adminEmail}");
    }
}
