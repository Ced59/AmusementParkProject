using System.Globalization;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkOpeningHours.Commands;
using AmusementPark.Application.Features.ParkOpeningHours.Queries;
using AmusementPark.Application.Features.ParkOpeningHours.Results;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.WebAPI.AdminPublicView;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Contracts.ParkOpeningHours;
using AmusementPark.WebAPI.Filters;
using AmusementPark.WebAPI.Mappers;
using AmusementPark.WebAPI.OutputCaching;
using AmusementPark.WebAPI.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace AmusementPark.WebAPI.Controllers;

[ApiController]
public sealed class ParkOpeningHoursController : ControllerBase
{
    private const string DateFormat = "yyyy-MM-dd";

    private readonly IQueryHandler<GetParkOpeningHoursCalendarQuery, ApplicationResult<ParkOpeningHoursCalendarResult>> getCalendarQueryHandler;
    private readonly IQueryHandler<GetParkOpeningHoursScheduleQuery, ApplicationResult<ParkOpeningHoursScheduleResult>> getScheduleQueryHandler;
    private readonly ICommandHandler<UpsertParkOpeningHoursScheduleCommand, ApplicationResult<ParkOpeningHoursSchedule>> upsertScheduleCommandHandler;

    public ParkOpeningHoursController(
        IQueryHandler<GetParkOpeningHoursCalendarQuery, ApplicationResult<ParkOpeningHoursCalendarResult>> getCalendarQueryHandler,
        IQueryHandler<GetParkOpeningHoursScheduleQuery, ApplicationResult<ParkOpeningHoursScheduleResult>> getScheduleQueryHandler,
        ICommandHandler<UpsertParkOpeningHoursScheduleCommand, ApplicationResult<ParkOpeningHoursSchedule>> upsertScheduleCommandHandler)
    {
        this.getCalendarQueryHandler = getCalendarQueryHandler;
        this.getScheduleQueryHandler = getScheduleQueryHandler;
        this.upsertScheduleCommandHandler = upsertScheduleCommandHandler;
    }

    [HttpGet("parks/{parkId}/opening-hours")]
    [AllowAnonymous]
    [OutputCache(PolicyName = ApiOutputCachePolicyNames.PublicDataMedium)]
    [ProducesResponseType(typeof(ParkOpeningHoursCalendarDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCalendarAsync(
        [FromRoute] string parkId,
        [FromQuery] string? from = null,
        [FromQuery] string? to = null,
        CancellationToken cancellationToken = default)
    {
        ApplicationResult<ParkOpeningHoursCalendarResult> result = await this.getCalendarQueryHandler.HandleAsync(
            new GetParkOpeningHoursCalendarQuery(
                parkId,
                ParseDate(from),
                ParseDate(to),
                this.HttpContext.UserCanSeeNonVisibleInPublicView()),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }

    [HttpGet("admin/parks/{parkId}/opening-hours")]
    [Authorize(Roles = AuthorizationRoleGroups.Admin)]
    [RequireActivatedUnblockedUser]
    [ProducesResponseType(typeof(ParkOpeningHoursScheduleDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAdminScheduleAsync([FromRoute] string parkId, CancellationToken cancellationToken = default)
    {
        ApplicationResult<ParkOpeningHoursScheduleResult> result = await this.getScheduleQueryHandler.HandleAsync(
            new GetParkOpeningHoursScheduleQuery(parkId, IncludeHidden: true),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }

    [HttpPut("admin/parks/{parkId}/opening-hours")]
    [Authorize(Roles = AuthorizationRoleGroups.Admin)]
    [RequireActivatedUnblockedUser]
    [AdminAudit("park-opening-hours.upsert", "Park", TargetIdRouteKey = "parkId")]
    [InvalidatesPublicCache(PublicCacheScope.Data, PublicCacheScope.Seo)]
    [ProducesResponseType(typeof(ParkOpeningHoursScheduleDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpsertAdminScheduleAsync(
        [FromRoute] string parkId,
        [FromBody] ParkOpeningHoursScheduleDto request,
        CancellationToken cancellationToken = default)
    {
        ApplicationResult<ParkOpeningHoursSchedule> mappingResult = request.ToDomainResult(parkId);
        if (!mappingResult.IsSuccess || mappingResult.Value is null)
        {
            return this.ToActionResult(mappingResult);
        }

        ApplicationResult<ParkOpeningHoursSchedule> result = await this.upsertScheduleCommandHandler.HandleAsync(
            new UpsertParkOpeningHoursScheduleCommand(mappingResult.Value),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }

    private static DateOnly? ParseDate(string? value)
    {
        return DateOnly.TryParseExact(value, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly parsed)
            ? parsed
            : null;
    }
}
