using System.ComponentModel.DataAnnotations;

namespace PurchaseOrderManagement.Api.Dtos.Companies;

public class UpdateCompanyRequest
{
    [Required]
    [StringLength(256, MinimumLength = 1)]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Null for a top-level company. Otherwise must reference an existing company and must not
    /// introduce a cycle (a company cannot be its own ancestor).
    /// </summary>
    public int? ParentCompanyId { get; set; }
}
