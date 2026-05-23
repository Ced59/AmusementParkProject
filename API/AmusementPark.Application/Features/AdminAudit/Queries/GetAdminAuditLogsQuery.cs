using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.AdminAudit.Models;
using AmusementPark.Application.Features.AdminAudit.Results;

namespace AmusementPark.Application.Features.AdminAudit.Queries;

/// <summary>
/// Recherche paginée des traces d'audit d'administration.
/// </summary>
public sealed record GetAdminAuditLogsQuery(AdminAuditLogSearchCriteria Criteria) : IQuery<ApplicationResult<PagedResult<AdminAuditLogResult>>>;
