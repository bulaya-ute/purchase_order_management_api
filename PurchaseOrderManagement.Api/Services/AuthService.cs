using Microsoft.EntityFrameworkCore;
using PurchaseOrderManagement.Api.Data;
using PurchaseOrderManagement.Api.Entities;

namespace PurchaseOrderManagement.Api.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher _passwordHasher;

    public AuthService(AppDbContext db, IPasswordHasher passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    public async Task<User?> ValidateCredentialsAsync(string email, string password, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (user is null || !user.IsActive)
        {
            return null;
        }

        return _passwordHasher.Verify(password, user.PasswordHash, user.PasswordSalt) ? user : null;
    }
}
