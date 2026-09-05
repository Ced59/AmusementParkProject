using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Results;
using AmusementPark.WebAPI.Contracts.PassportBeta;

namespace AmusementPark.WebAPI.Mappers;

public static class PassportBetaMetricsHttpMapper
{
    public static PassportBetaMetricsDto ToHttp(this PassportBetaMetricsResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return new PassportBetaMetricsDto
        {
            GeneratedAtUtc = result.GeneratedAtUtc,
            FromUtc = result.FromUtc,
            ToUtc = result.ToUtc,
            CreatedVisits = result.CreatedVisits,
            CompletedVisits = result.CompletedVisits,
            UsersWithCompletedVisit = result.UsersWithCompletedVisit,
            UsersWithSecondCompletedVisit = result.UsersWithSecondCompletedVisit,
            RepeatUsageRatePercent = result.RepeatUsageRatePercent,
            RepeatUsageSignal = result.RepeatUsageSignal.ToString(),
            RequiresQualitativeValidation = result.RequiresQualitativeValidation,
            Daily = result.Daily.Select(ToHttp).ToList(),
        };
    }

    private static PassportBetaDailyMetricsDto ToHttp(PassportBetaDailyMetrics metrics)
    {
        return new PassportBetaDailyMetricsDto
        {
            Date = metrics.Date,
            CompletedVisits = metrics.CompletedVisits,
            FirstVisits = metrics.FirstVisits,
            SecondVisits = metrics.SecondVisits,
        };
    }
}
