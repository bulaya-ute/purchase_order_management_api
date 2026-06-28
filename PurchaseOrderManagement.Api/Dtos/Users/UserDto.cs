namespace PurchaseOrderManagement.Api.Dtos.Users;

/// <summary>
/// Resolved user view. Never includes the password hash/salt.
/// </summary>
public class UserDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public bool IsActive { get; set; }
    public int CompanyId { get; set; }
    public string CompanyName { get; set; } = null!;
    public IReadOnlyList<UserRoleDto> Roles { get; set; } = Array.Empty<UserRoleDto>();
}

public class UserRoleDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
}
