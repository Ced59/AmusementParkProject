using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.AdminAudit.Queries;
using AmusementPark.Application.Features.AdminAudit.Results;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Contracts.AdminAudit;
using AmusementPark.WebAPI.Contracts.Common;
using AmusementPark.WebAPI.Filters;
using AmusementPark.WebAPI.Mappers;
using AmusementPark.WebAPI.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AmusementPark.WebAPI.Controllers;

/// <summary>
/// Consultation lecture seule des traces d'audit d'administration.
/// </summary>
[ApiController]
[Route("admin/audit-logs")]
[Authorize(Roles = AuthorizationRoleGroups.Admin)]
[RequireActivatedUnblockedUser]
public sealed class AdminAuditLogsController : ControllerBase
{
    private readonly IQueryHandler<GetAdminAuditLogsQuery, ApplicationResult<PagedResult<AdminAuditLogResult>>> getAdminAuditLogsQueryHandler;

    public AdminAuditLogsController(IQueryHandler<GetAdminAuditLogsQuery, ApplicationResult<PagedResult<AdminAuditLogResult>>> getAdminAuditLogsQueryHandler)
    {
        this.getAdminAuditLogsQueryHandler = getAdminAuditLogsQueryHandler;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponseDto<AdminAuditLogDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchAsync([FromQuery] AdminAuditLogSearchRequestDto request, CancellationToken cancellationToken = default)
    {
        ApplicationResult<PagedResult<AdminAuditLogResult>> result = await this.getAdminAuditLogsQueryHandler.HandleAsync(
            new GetAdminAuditLogsQuery(request.ToApplication()),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToPagedResponse(static log => log.ToHttp()));
    }
}
