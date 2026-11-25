namespace JobAutoApplyNET.Domain.Common;

/// <summary>
/// Base entity providing ubiquitous audit and tracking identifiers across domain models.
/// </summary>
public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
