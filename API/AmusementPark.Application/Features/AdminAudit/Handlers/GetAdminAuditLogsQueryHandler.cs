using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.AdminAudit.Ports;
using AmusementPark.Application.Features.AdminAudit.Queries;
using AmusementPark.Application.Features.AdminAudit.Results;

namespace AmusementPark.Application.Features.AdminAudit.Handlers;

/// <summary>
/// Handler de consultation paginée des traces d'audit d'administration.
/// </summary>
public sealed class GetAdminAuditLogsQueryHandler : IQueryHandler<GetAdminAuditLogsQuery, ApplicationResult<PagedResult<AdminAuditLogResult>>>
{
    private readonly IAdminAuditLogReader reader;

    public GetAdminAuditLogsQueryHandler(IAdminAuditLogReader reader)
    {
        this.reader = reader;
    }

    public async Task<ApplicationResult<PagedResult<AdminAuditLogResult>>> HandleAsync(GetAdminAuditLogsQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.Criteria.Paging.Page <= 0 || query.Criteria.Paging.PageSize <= 0 || query.Criteria.Paging.PageSize > 100)
        {
            return ApplicationResult<PagedResult<AdminAuditLogResult>>.Failure(ApplicationErrors.InvalidPagination());
        }

        if (query.Criteria.FromUtc.HasValue && query.Criteria.ToUtc.HasValue && query.Criteria.FromUtc.Value > query.Criteria.ToUtc.Value)
        {
            return ApplicationResult<PagedResult<AdminAuditLogResult>>.Failure(
                ApplicationError.Validation(
                    "admin-audit.date-range.invalid",
                    "La date de début doit être antérieure ou égale à la date de fin."));
        }

        PagedResult<AdminAuditLogResult> page = await this.reader.SearchAsync(query.Criteria, cancellationToken);
        return ApplicationResult<PagedResult<AdminAuditLogResult>>.Success(page);
    }
}
