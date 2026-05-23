using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.AdminAudit.Models;
using AmusementPark.Application.Features.AdminAudit.Results;

namespace AmusementPark.Application.Features.AdminAudit.Ports;

/// <summary>
/// Port applicatif de lecture des traces d'audit d'administration.
/// </summary>
public interface IAdminAuditLogReader
{
    Task<PagedResult<AdminAuditLogResult>> SearchAsync(AdminAuditLogSearchCriteria criteria, CancellationToken cancellationToken);
}
