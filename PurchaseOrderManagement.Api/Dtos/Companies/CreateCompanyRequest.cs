using System.ComponentModel.DataAnnotations;

namespace PurchaseOrderManagement.Api.Dtos.Companies;

public class CreateCompanyRequest
{
    [Required]
    [StringLength(256, MinimumLength = 1)]
    public string Name { get; set; } = null!;

    /// <summary>Null for a top-level company. Otherwise must reference an existing company.</summary>
    public int? ParentCompanyId { get; set; }
}
