namespace IntegrationService.Domain.Common;

/// <summary>
/// Base entity — giống iolis-instrument EntityBase.
/// Mọi entity đều kế thừa class này.
/// </summary>
public abstract class EntityBase
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string? CreatedBy { get; set; }
    public DateTime? CreatedDate { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime? UpdatedDate { get; set; }
}
