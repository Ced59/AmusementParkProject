using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Application.Features.Passport.Queries;
using AmusementPark.Application.Features.Passport.Results;

namespace AmusementPark.Application.Features.Passport.Handlers;

public sealed class GetPassportBetaMetricsQueryHandler
    : IQueryHandler<GetPassportBetaMetricsQuery, ApplicationResult<PassportBetaMetricsResult>>
{
    private const int CandidateReturningUserCount = 3;
    private static readonly TimeSpan DefaultRange = TimeSpan.FromDays(30);
    private static readonly TimeSpan MaximumRange = TimeSpan.FromDays(180);

    private readonly IPassportBetaMetricsSource source;
    private readonly IPassportClock clock;

    public GetPassportBetaMetricsQueryHandler(
        IPassportBetaMetricsSource source,
        IPassportClock clock)
    {
        this.source = source;
        this.clock = clock;
    }

    public async Task<ApplicationResult<PassportBetaMetricsResult>> HandleAsync(
        GetPassportBetaMetricsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        DateTime generatedAtUtc = this.clock.UtcNow;
        DateTime toUtc = NormalizeUtc(query.ToUtc) ?? generatedAtUtc;
        DateTime? requestedFromUtc = NormalizeUtc(query.FromUtc);
        if (!requestedFromUtc.HasValue && toUtc < DateTime.MinValue.Add(DefaultRange))
        {
            return ApplicationResult<PassportBetaMetricsResult>.Failure(
                PassportApplicationErrors.InvalidBetaMetricsDateRange());
        }

        DateTime fromUtc = requestedFromUtc ?? toUtc.Subtract(DefaultRange);
        if (fromUtc > toUtc)
        {
            return ApplicationResult<PassportBetaMetricsResult>.Failure(
                PassportApplicationErrors.InvalidBetaMetricsDateRange());
        }

        if (toUtc.Subtract(fromUtc) > MaximumRange)
        {
            fromUtc = toUtc.Subtract(MaximumRange);
        }

        PassportBetaMetricsSourceSnapshot snapshot = await this.source.ReadAsync(
            fromUtc,
            toUtc,
            cancellationToken);
        decimal repeatUsageRatePercent = snapshot.UsersWithCompletedVisit == 0
            ? 0m
            : snapshot.UsersWithSecondCompletedVisit * 100m
                / snapshot.UsersWithCompletedVisit;
        PassportBetaRepeatUsageSignal repeatUsageSignal = ResolveRepeatUsageSignal(
            snapshot.UsersWithSecondCompletedVisit);

        PassportBetaMetricsResult result = new PassportBetaMetricsResult(
            generatedAtUtc,
            fromUtc,
            toUtc,
            snapshot.CreatedVisits,
            snapshot.CompletedVisits,
            snapshot.UsersWithCompletedVisit,
            snapshot.UsersWithSecondCompletedVisit,
            repeatUsageRatePercent,
            repeatUsageSignal,
            true,
            snapshot.Daily);
        return ApplicationResult<PassportBetaMetricsResult>.Success(result);
    }

    private static DateTime? NormalizeUtc(DateTime? value)
    {
        if (!value.HasValue)
        {
            return null;
        }

        return value.Value.Kind == DateTimeKind.Utc
            ? value.Value
            : value.Value.ToUniversalTime();
    }

    private static PassportBetaRepeatUsageSignal ResolveRepeatUsageSignal(long returningUsers)
    {
        if (returningUsers <= 0)
        {
            return PassportBetaRepeatUsageSignal.NotObserved;
        }

        return returningUsers < CandidateReturningUserCount
            ? PassportBetaRepeatUsageSignal.Emerging
            : PassportBetaRepeatUsageSignal.Candidate;
    }
}
