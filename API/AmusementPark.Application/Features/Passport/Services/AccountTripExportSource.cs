using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Application.Features.Trips.Ports;
using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.Application.Features.Trips.Services;
using AmusementPark.Core.Domain.Trips;

namespace AmusementPark.Application.Features.Passport.Services;

public sealed class AccountTripExportSource : IAccountTripExportSource
{
    private readonly ITripPlanRepository tripPlans;
    private readonly TripExportService tripExportService;

    public AccountTripExportSource(
        ITripPlanRepository tripPlans,
        TripExportService tripExportService)
    {
        this.tripPlans = tripPlans;
        this.tripExportService = tripExportService;
    }

    public async Task<IReadOnlyCollection<AccountTripExportData>> LoadAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<TripPlan> plans = await this.tripPlans.ListAccessibleAsync(
            userId,
            cancellationToken);
        List<AccountTripExportData> results = new List<AccountTripExportData>();
        int position = 0;
        foreach (TripPlan plan in plans.OrderBy(static value => value.CreatedAtUtc))
        {
            TripEffectiveRole? role = plan.ResolveRole(userId);
            if (!role.HasValue
                || !TripAuthorizationPolicy.HasPermission(role.Value, TripPermission.Export))
            {
                continue;
            }

            ApplicationResult<TripExportResult> exportResult =
                await this.tripExportService.BuildPortableAsync(
                    userId,
                    plan.Id.Value,
                    cancellationToken);
            if (!exportResult.IsSuccess || exportResult.Value is null)
            {
                throw new InvalidOperationException("An accessible trip could not be exported.");
            }

            position++;
            results.Add(new AccountTripExportData(
                $"trip-{position:D4}",
                role.Value.ToString(),
                exportResult.Value));
        }

        return results;
    }
}
