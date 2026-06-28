using Microsoft.EntityFrameworkCore;
using PurchaseOrderManagement.Api.Data;
using PurchaseOrderManagement.Api.Dtos.Common;
using PurchaseOrderManagement.Api.Dtos.Users;
using PurchaseOrderManagement.Api.Entities;

namespace PurchaseOrderManagement.Api.Services;

public class UserService : IUserService
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher _passwordHasher;

    public UserService(AppDbContext db, IPasswordHasher passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    public async Task<PagedResult<UserDto>> ListAsync(UserListQuery query, CancellationToken cancellationToken)
    {
        var baseQuery = _db.Users.AsNoTracking();

        if (query.CompanyId is int companyId)
        {
            baseQuery = baseQuery.Where(u => u.CompanyId == companyId);
        }

        var ordered = baseQuery.OrderBy(u => u.FullName).ThenBy(u => u.Id);

        var totalCount = await ordered.CountAsync(cancellationToken);

        var items = await ordered
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(ProjectToDto)
            .ToListAsync(cancellationToken);

        return new PagedResult<UserDto>
        {
            Items = items,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount,
        };
    }

    public async Task<UserDto?> GetAsync(int id, CancellationToken cancellationToken)
    {
        return await _db.Users.AsNoTracking()
            .Where(u => u.Id == id)
            .Select(ProjectToDto)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim();

        await EnsureCompanyExistsAsync(request.CompanyId, cancellationToken);
        await EnsureEmailUniqueAsync(email, excludeUserId: null, cancellationToken);

        var roleIds = Distinct(request.RoleIds);
        await EnsureRolesExistAsync(roleIds, cancellationToken);

        var (hash, salt) = _passwordHasher.Create(request.Password);

        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = email,
            CompanyId = request.CompanyId,
            IsActive = request.IsActive,
            PasswordHash = hash,
            PasswordSalt = salt,
        };

        foreach (var roleId in roleIds)
        {
            user.UserRoles.Add(new UserRole { RoleId = roleId });
        }

        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);

        return (await GetAsync(user.Id, cancellationToken))!;
    }

    public async Task<UserDto> UpdateAsync(int id, UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken)
            ?? throw ServiceException.NotFound($"User {id} was not found.");

        var email = request.Email.Trim();

        await EnsureCompanyExistsAsync(request.CompanyId, cancellationToken);
        await EnsureEmailUniqueAsync(email, excludeUserId: id, cancellationToken);

        var desiredRoleIds = Distinct(request.RoleIds);
        await EnsureRolesExistAsync(desiredRoleIds, cancellationToken);

        user.FullName = request.FullName.Trim();
        user.Email = email;
        user.CompanyId = request.CompanyId;
        user.IsActive = request.IsActive;

        ReconcileRoles(user, desiredRoleIds);

        await _db.SaveChangesAsync(cancellationToken);

        return (await GetAsync(user.Id, cancellationToken))!;
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken)
            ?? throw ServiceException.NotFound($"User {id} was not found.");

        // Soft-delete the role links alongside the user (Remove() -> soft delete via AppDbContext).
        foreach (var link in user.UserRoles.ToList())
        {
            _db.UserRoles.Remove(link);
        }

        _db.Users.Remove(user);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task ResetPasswordAsync(int id, ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken)
            ?? throw ServiceException.NotFound($"User {id} was not found.");

        var (hash, salt) = _passwordHasher.Create(request.NewPassword);
        user.PasswordHash = hash;
        user.PasswordSalt = salt;

        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Reconciles the user's UserRoles to match the desired set: adds missing links, soft-deletes
    /// the links that are no longer wanted. Existing wanted links are left untouched.
    /// </summary>
    private void ReconcileRoles(User user, IReadOnlyCollection<int> desiredRoleIds)
    {
        var existingByRoleId = user.UserRoles.ToDictionary(ur => ur.RoleId);

        foreach (var link in user.UserRoles.ToList())
        {
            if (!desiredRoleIds.Contains(link.RoleId))
            {
                _db.UserRoles.Remove(link);
            }
        }

        foreach (var roleId in desiredRoleIds)
        {
            if (!existingByRoleId.ContainsKey(roleId))
            {
                user.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = roleId });
            }
        }
    }

    private async Task EnsureCompanyExistsAsync(int companyId, CancellationToken cancellationToken)
    {
        var exists = await _db.Companies.AnyAsync(c => c.Id == companyId, cancellationToken);
        if (!exists)
        {
            throw ServiceException.Validation($"Company {companyId} was not found.");
        }
    }

    private async Task EnsureEmailUniqueAsync(string email, int? excludeUserId, CancellationToken cancellationToken)
    {
        // Email column is citext, so EF translates this to a case-insensitive comparison server-side.
        var clash = await _db.Users
            .AnyAsync(u => u.Email == email && (excludeUserId == null || u.Id != excludeUserId), cancellationToken);

        if (clash)
        {
            throw ServiceException.Conflict($"A user with email '{email}' already exists.");
        }
    }

    private async Task EnsureRolesExistAsync(IReadOnlyCollection<int> roleIds, CancellationToken cancellationToken)
    {
        if (roleIds.Count == 0)
        {
            return;
        }

        var foundCount = await _db.Roles.CountAsync(r => roleIds.Contains(r.Id), cancellationToken);
        if (foundCount != roleIds.Count)
        {
            throw ServiceException.Validation("One or more of the specified roles do not exist.");
        }
    }

    private static IReadOnlyList<int> Distinct(IReadOnlyList<int> roleIds) =>
        roleIds.Distinct().ToList();

    private static readonly System.Linq.Expressions.Expression<Func<User, UserDto>> ProjectToDto =
        u => new UserDto
        {
            Id = u.Id,
            FullName = u.FullName,
            Email = u.Email,
            IsActive = u.IsActive,
            CompanyId = u.CompanyId,
            CompanyName = u.Company.Name,
            Roles = u.UserRoles
                .Select(ur => new UserRoleDto { Id = ur.RoleId, Name = ur.Role.Name })
                .ToList(),
        };
}
