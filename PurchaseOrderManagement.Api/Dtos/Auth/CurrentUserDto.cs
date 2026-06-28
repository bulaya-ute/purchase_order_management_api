namespace PurchaseOrderManagement.Api.Dtos.Auth;

public class CurrentUserDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public int CompanyId { get; set; }
    public IReadOnlyCollection<string> Roles { get; set; } = Array.Empty<string>();
}
