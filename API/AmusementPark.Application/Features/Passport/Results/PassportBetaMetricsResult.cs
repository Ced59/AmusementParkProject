using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Results;

public sealed record PassportBetaMetricsResult(
    DateTime GeneratedAtUtc,
    DateTime FromUtc,
    DateTime ToUtc,
    long CreatedVisits,
    long CompletedVisits,
    long UsersWithCompletedVisit,
    long UsersWithSecondCompletedVisit,
    decimal RepeatUsageRatePercent,
    PassportBetaRepeatUsageSignal RepeatUsageSignal,
    bool RequiresQualitativeValidation,
    IReadOnlyCollection<PassportBetaDailyMetrics> Daily);
