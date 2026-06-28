namespace PurchaseOrderManagement.Api.Dtos.Roles;

/// <summary>
/// Flat role representation including ParentRoleId so the client can build the tree.
/// </summary>
public class RoleDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public int? ParentRoleId { get; set; }
    public bool IsSystemRole { get; set; }
}
