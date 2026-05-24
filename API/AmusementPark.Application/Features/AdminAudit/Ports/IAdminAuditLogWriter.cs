using AmusementPark.Application.Features.AdminAudit.Models;

namespace AmusementPark.Application.Features.AdminAudit.Ports;

/// <summary>
/// Port applicatif de persistance des traces d'audit d'administration.
/// </summary>
public interface IAdminAuditLogWriter
{
    /// <summary>
    /// Persiste une trace d'audit.
    /// </summary>
    Task WriteAsync(AdminAuditLogEntry entry, CancellationToken cancellationToken);
}
