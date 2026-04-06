namespace AmusementPark.Core.Abstractions;

/// <summary>
/// Représente une entité de domaine auditée.
/// </summary>
public abstract class AuditableEntity : EntityBase
{
    /// <summary>
    /// Date de création UTC.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Date de dernière mise à jour UTC.
    /// </summary>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Met à jour le timestamp de dernière modification.
    /// </summary>
    public void Touch()
    {
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
