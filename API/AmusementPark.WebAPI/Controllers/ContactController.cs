using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Contact.Commands;
using AmusementPark.Application.Features.Contact.Contracts;
using AmusementPark.Application.Features.Contact.Queries;
using AmusementPark.Core.Domain.Contact;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Contracts.Common;
using AmusementPark.WebAPI.Contracts.Contact;
using AmusementPark.WebAPI.Filters;
using AmusementPark.WebAPI.Mappers;
using AmusementPark.WebAPI.RateLimiting;
using AmusementPark.WebAPI.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AmusementPark.WebAPI.Controllers;

[ApiController]
[Route("contact")]
public sealed class ContactController : ControllerBase
{
    private readonly ICommandHandler<SubmitContactGrievanceCommand, ApplicationResult<ContactGrievanceSubmissionResult>> submitContactGrievanceCommandHandler;
    private readonly IQueryHandler<GetContactGrievancesQuery, ApplicationResult<PagedResult<ContactGrievance>>> getContactGrievancesQueryHandler;

    public ContactController(
        ICommandHandler<SubmitContactGrievanceCommand, ApplicationResult<ContactGrievanceSubmissionResult>> submitContactGrievanceCommandHandler,
        IQueryHandler<GetContactGrievancesQuery, ApplicationResult<PagedResult<ContactGrievance>>> getContactGrievancesQueryHandler)
    {
        this.submitContactGrievanceCommandHandler = submitContactGrievanceCommandHandler;
        this.getContactGrievancesQueryHandler = getContactGrievancesQueryHandler;
    }

    [HttpPost("grievances")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicyNames.ContactSubmission)]
    [ProducesResponseType(typeof(ContactGrievanceSubmissionDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> SubmitGrievanceAsync([FromBody] SubmitContactGrievanceRequest? request, CancellationToken cancellationToken = default)
    {
        string ipAddress = this.ResolveRemoteIpAddress();
        string? userAgent = this.Request.Headers["User-Agent"].ToString();
        SubmitContactGrievanceRequest safeRequest = request ?? new SubmitContactGrievanceRequest();
        ApplicationResult<ContactGrievanceSubmissionResult> result = await this.submitContactGrievanceCommandHandler.HandleAsync(
            new SubmitContactGrievanceCommand(safeRequest.ToApplication(ipAddress, userAgent)),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }

    [HttpGet("/admin/contact/grievances")]
    [RequireActivatedUnblockedUser]
    [Authorize(Roles = AuthorizationRoleGroups.Admin)]
    [ProducesResponseType(typeof(PagedResponseDto<AdminContactGrievanceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAdminGrievancesAsync(
        [FromQuery] PaginationRequestDto pagination,
        [FromQuery] string? search = null,
        [FromQuery] string? ipAddress = null,
        [FromQuery] string? languageCode = null,
        CancellationToken cancellationToken = default)
    {
        ContactGrievanceSearchCriteria criteria = new ContactGrievanceSearchCriteria(search, ipAddress, languageCode);
        ApplicationResult<PagedResult<ContactGrievance>> result = await this.getContactGrievancesQueryHandler.HandleAsync(
            new GetContactGrievancesQuery(pagination.ToApplication(), criteria),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToPagedResponse(static grievance => grievance.ToAdminHttp()));
    }

    private string ResolveRemoteIpAddress()
    {
        return this.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
