using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.FeatureFlags.Results;
using AmusementPark.Application.Features.FeatureFlags.Services;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Contracts.FeatureFlags;
using AmusementPark.WebAPI.Extensions;
using AmusementPark.WebAPI.Filters;
using AmusementPark.WebAPI.Mappers;
using AmusementPark.WebAPI.RateLimiting;
using AmusementPark.WebAPI.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AmusementPark.WebAPI.Controllers;

[ApiController]
[Route("admin/feature-flags")]
[Authorize(Roles = AuthorizationRoleGroups.Admin)]
[RequireActivatedUnblockedUser]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AdminFeatureFlagsController : ControllerBase
{
    private readonly FeatureFlagAdministrationService service;

    public AdminFeatureFlagsController(FeatureFlagAdministrationService service)
    {
        this.service = service;
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(IReadOnlyCollection<FeatureFlagAdministrationDto>),
        StatusCodes.Status200OK)]
    public async Task<IActionResult> ListAsync(CancellationToken cancellationToken = default)
    {
        ApplicationResult<IReadOnlyCollection<FeatureFlagAdministrationResult>> result =
            await this.service.ListAsync(cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.Select(static item => item.ToHttp()).ToArray())
            : this.ToActionResult(result);
    }

    [HttpPut("{key}")]
    [EnableRateLimiting(RateLimitPolicyNames.LiveDataAdministration)]
    [AdminAudit(
        "feature-flag.override.update",
        "FeatureFlag",
        TargetIdRouteKey = "key")]
    [ProducesResponseType(typeof(FeatureFlagAdministrationDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateAsync(
        string key,
        [FromBody] UpdateFeatureFlagRequestDto request,
        CancellationToken cancellationToken = default)
    {
        string? changedByUserId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(changedByUserId))
        {
            return this.BadRequest();
        }

        ApplicationResult<FeatureFlagAdministrationResult> result =
            await this.service.UpdateAsync(
                key,
                request.EnabledOverride,
                request.ExpectedRevision,
                request.Reason,
                changedByUserId,
                cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToHttp())
            : this.ToActionResult(result);
    }
}
