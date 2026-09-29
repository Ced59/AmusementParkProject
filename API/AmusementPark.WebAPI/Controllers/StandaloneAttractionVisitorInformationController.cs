using System.Globalization;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkOpeningHours.Results;
using AmusementPark.Application.Features.ParkWeather.Results;
using AmusementPark.Application.Features.StandaloneAttractions.Commands;
using AmusementPark.Application.Features.StandaloneAttractions.Queries;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.WebAPI.AdminPublicView;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Contracts.ParkOpeningHours;
using AmusementPark.WebAPI.Contracts.ParkPricing;
using AmusementPark.WebAPI.Contracts.ParkWeather;
using AmusementPark.WebAPI.Filters;
using AmusementPark.WebAPI.Mappers;
using AmusementPark.WebAPI.OutputCaching;
using AmusementPark.WebAPI.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using ParkPricingEntity = AmusementPark.Core.Domain.Parks.ParkPricing;

namespace AmusementPark.WebAPI.Controllers;

[ApiController]
public sealed class StandaloneAttractionVisitorInformationController : ControllerBase
{
    private const string DateFormat = "yyyy-MM-dd";

    private readonly IQueryHandler<GetStandaloneAttractionOpeningHoursCalendarQuery, ApplicationResult<ParkOpeningHoursCalendarResult>> getCalendarHandler;
    private readonly IQueryHandler<GetStandaloneAttractionOpeningHoursScheduleQuery, ApplicationResult<ParkOpeningHoursScheduleResult>> getScheduleHandler;
    private readonly ICommandHandler<UpsertStandaloneAttractionOpeningHoursCommand, ApplicationResult<ParkOpeningHoursSchedule>> upsertScheduleHandler;
    private readonly IQueryHandler<GetStandaloneAttractionPricingQuery, ApplicationResult<ParkPricingEntity>> getPricingHandler;
    private readonly ICommandHandler<UpsertStandaloneAttractionPricingCommand, ApplicationResult<ParkPricingEntity>> upsertPricingHandler;
    private readonly IQueryHandler<GetStandaloneAttractionWeatherForecastQuery, ApplicationResult<ParkWeatherForecastResult>> getWeatherHandler;

    public StandaloneAttractionVisitorInformationController(
        IQueryHandler<GetStandaloneAttractionOpeningHoursCalendarQuery, ApplicationResult<ParkOpeningHoursCalendarResult>> getCalendarHandler,
        IQueryHandler<GetStandaloneAttractionOpeningHoursScheduleQuery, ApplicationResult<ParkOpeningHoursScheduleResult>> getScheduleHandler,
        ICommandHandler<UpsertStandaloneAttractionOpeningHoursCommand, ApplicationResult<ParkOpeningHoursSchedule>> upsertScheduleHandler,
        IQueryHandler<GetStandaloneAttractionPricingQuery, ApplicationResult<ParkPricingEntity>> getPricingHandler,
        ICommandHandler<UpsertStandaloneAttractionPricingCommand, ApplicationResult<ParkPricingEntity>> upsertPricingHandler,
        IQueryHandler<GetStandaloneAttractionWeatherForecastQuery, ApplicationResult<ParkWeatherForecastResult>> getWeatherHandler)
    {
        this.getCalendarHandler = getCalendarHandler;
        this.getScheduleHandler = getScheduleHandler;
        this.upsertScheduleHandler = upsertScheduleHandler;
        this.getPricingHandler = getPricingHandler;
        this.upsertPricingHandler = upsertPricingHandler;
        this.getWeatherHandler = getWeatherHandler;
    }

    [HttpGet("standalone-attractions/{standaloneAttractionId}/opening-hours")]
    [AllowAnonymous]
    [OutputCache(PolicyName = ApiOutputCachePolicyNames.PublicDataMedium)]
    [ProducesResponseType(typeof(ParkOpeningHoursCalendarDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOpeningHoursAsync(
        [FromRoute] string standaloneAttractionId,
        [FromQuery] string? from = null,
        [FromQuery] string? to = null,
        CancellationToken cancellationToken = default)
    {
        ApplicationResult<ParkOpeningHoursCalendarResult> result =
            await this.getCalendarHandler.HandleAsync(
                new GetStandaloneAttractionOpeningHoursCalendarQuery(
                    standaloneAttractionId,
                    ParseDate(from),
                    ParseDate(to),
                    this.HttpContext.UserCanSeeNonVisibleInPublicView()),
                cancellationToken);

        return !result.IsSuccess || result.Value is null
            ? this.ToActionResult(result)
            : this.Ok(result.Value.ToHttp());
    }

    [HttpGet("standalone-attractions/{standaloneAttractionId}/pricing")]
    [AllowAnonymous]
    [OutputCache(PolicyName = ApiOutputCachePolicyNames.PublicPricingData)]
    [ProducesResponseType(typeof(ParkPricingDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPricingAsync(
        [FromRoute] string standaloneAttractionId,
        CancellationToken cancellationToken = default)
    {
        ApplicationResult<ParkPricingEntity> result = await this.getPricingHandler.HandleAsync(
            new GetStandaloneAttractionPricingQuery(
                standaloneAttractionId,
                this.HttpContext.UserCanSeeNonVisibleInPublicView()),
            cancellationToken);

        return !result.IsSuccess || result.Value is null
            ? this.ToActionResult(result)
            : this.Ok(result.Value.ToPublicHttp());
    }

    [HttpGet("standalone-attractions/{standaloneAttractionId}/weather")]
    [AllowAnonymous]
    [OutputCache(PolicyName = ApiOutputCachePolicyNames.PublicWeatherDataShort)]
    [ProducesResponseType(typeof(ParkWeatherForecastDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWeatherAsync(
        [FromRoute] string standaloneAttractionId,
        [FromQuery] int days = 7,
        CancellationToken cancellationToken = default)
    {
        ApplicationResult<ParkWeatherForecastResult> result = await this.getWeatherHandler.HandleAsync(
            new GetStandaloneAttractionWeatherForecastQuery(standaloneAttractionId, days),
            cancellationToken);

        return !result.IsSuccess || result.Value is null
            ? this.ToActionResult(result)
            : this.Ok(result.Value.ToHttp());
    }

    [HttpGet("admin/standalone-attractions/{standaloneAttractionId}/opening-hours")]
    [Authorize(Roles = AuthorizationRoleGroups.Admin)]
    [RequireActivatedUnblockedUser]
    [ProducesResponseType(typeof(ParkOpeningHoursScheduleDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAdminOpeningHoursAsync(
        [FromRoute] string standaloneAttractionId,
        CancellationToken cancellationToken = default)
    {
        ApplicationResult<ParkOpeningHoursScheduleResult> result =
            await this.getScheduleHandler.HandleAsync(
                new GetStandaloneAttractionOpeningHoursScheduleQuery(
                    standaloneAttractionId,
                    IncludeHidden: true),
                cancellationToken);

        return !result.IsSuccess || result.Value is null
            ? this.ToActionResult(result)
            : this.Ok(result.Value.ToHttp());
    }

    [HttpPut("admin/standalone-attractions/{standaloneAttractionId}/opening-hours")]
    [Authorize(Roles = AuthorizationRoleGroups.Admin)]
    [RequireActivatedUnblockedUser]
    [AdminAudit("standalone-attraction-opening-hours.upsert", "StandaloneAttraction", TargetIdRouteKey = "standaloneAttractionId")]
    [InvalidatesPublicCache(PublicCacheScope.Data, PublicCacheScope.Seo)]
    [ProducesResponseType(typeof(ParkOpeningHoursScheduleDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpsertAdminOpeningHoursAsync(
        [FromRoute] string standaloneAttractionId,
        [FromBody] ParkOpeningHoursScheduleDto request,
        CancellationToken cancellationToken = default)
    {
        ApplicationResult<ParkOpeningHoursSchedule> mappingResult =
            request.ToDomainResult(standaloneAttractionId);
        if (!mappingResult.IsSuccess || mappingResult.Value is null)
        {
            return this.ToActionResult(mappingResult);
        }

        ApplicationResult<ParkOpeningHoursSchedule> result =
            await this.upsertScheduleHandler.HandleAsync(
                new UpsertStandaloneAttractionOpeningHoursCommand(mappingResult.Value),
                cancellationToken);
        return !result.IsSuccess || result.Value is null
            ? this.ToActionResult(result)
            : this.Ok(result.Value.ToHttp());
    }

    [HttpGet("admin/standalone-attractions/{standaloneAttractionId}/pricing")]
    [Authorize(Roles = AuthorizationRoleGroups.Admin)]
    [RequireActivatedUnblockedUser]
    [ProducesResponseType(typeof(ParkPricingDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAdminPricingAsync(
        [FromRoute] string standaloneAttractionId,
        CancellationToken cancellationToken = default)
    {
        ApplicationResult<ParkPricingEntity> result = await this.getPricingHandler.HandleAsync(
            new GetStandaloneAttractionPricingQuery(
                standaloneAttractionId,
                IncludeHidden: true),
            cancellationToken);
        return !result.IsSuccess || result.Value is null
            ? this.ToActionResult(result)
            : this.Ok(result.Value.ToHttp());
    }

    [HttpPut("admin/standalone-attractions/{standaloneAttractionId}/pricing")]
    [Authorize(Roles = AuthorizationRoleGroups.Admin)]
    [RequireActivatedUnblockedUser]
    [AdminAudit("standalone-attraction-pricing.upsert", "StandaloneAttraction", TargetIdRouteKey = "standaloneAttractionId")]
    [InvalidatesPublicCache(PublicCacheScope.Data, PublicCacheScope.Seo)]
    [ProducesResponseType(typeof(ParkPricingDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpsertAdminPricingAsync(
        [FromRoute] string standaloneAttractionId,
        [FromBody] ParkPricingDto request,
        CancellationToken cancellationToken = default)
    {
        ApplicationResult<ParkPricingEntity> mappingResult =
            request.ToDomainResult(standaloneAttractionId);
        if (!mappingResult.IsSuccess || mappingResult.Value is null)
        {
            return this.ToActionResult(mappingResult);
        }

        ApplicationResult<ParkPricingEntity> result = await this.upsertPricingHandler.HandleAsync(
            new UpsertStandaloneAttractionPricingCommand(
                mappingResult.Value,
                PreserveHistoricalSnapshots: request.HistoricalSnapshots is null,
                PreserveCreditOffers: request.CreditOffers is null),
            cancellationToken);
        return !result.IsSuccess || result.Value is null
            ? this.ToActionResult(result)
            : this.Ok(result.Value.ToHttp());
    }

    private static DateOnly? ParseDate(string? value)
    {
        return DateOnly.TryParseExact(
            value,
            DateFormat,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out DateOnly parsed)
            ? parsed
            : null;
    }
}
