namespace PurchaseOrderManagement.Api.Services;

/// <inheritdoc />
public class AdminAuthorizer : IAdminAuthorizer
{
    /// <summary>
    /// Admin-tier role names. Matches the seeded system roles from docs/01-IDENTITY-AND-ROLES.md.
    /// Case-insensitive to be forgiving of seed/display differences.
    /// </summary>
    private static readonly HashSet<string> AdminRoleNames =
        new(StringComparer.OrdinalIgnoreCase) { "Super Admin", "Admin" };

    private readonly ICurrentUser _currentUser;

    public AdminAuthorizer(ICurrentUser currentUser)
    {
        _currentUser = currentUser;
    }

    public bool IsAdmin() => _currentUser.Roles.Any(AdminRoleNames.Contains);
}
