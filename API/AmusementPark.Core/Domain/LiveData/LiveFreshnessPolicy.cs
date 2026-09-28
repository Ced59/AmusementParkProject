namespace AmusementPark.Core.Domain.LiveData;

public sealed class LiveFreshnessPolicy
{
    public static readonly TimeSpan MaximumFutureSkew = TimeSpan.FromHours(1);
    public static readonly TimeSpan MaximumStaleWindow = TimeSpan.FromDays(1);

    public LiveFreshnessPolicy(
        string version,
        TimeSpan freshUntil,
        TimeSpan agingUntil,
        TimeSpan staleUntil,
        TimeSpan acceptedFutureSkew)
    {
        string normalizedVersion = NormalizeVersion(version);
        ValidateThresholds(freshUntil, agingUntil, staleUntil, acceptedFutureSkew);

        this.Version = normalizedVersion;
        this.FreshUntil = freshUntil;
        this.AgingUntil = agingUntil;
        this.StaleUntil = staleUntil;
        this.AcceptedFutureSkew = acceptedFutureSkew;
    }

    public string Version { get; }

    public TimeSpan FreshUntil { get; }

    public TimeSpan AgingUntil { get; }

    public TimeSpan StaleUntil { get; }

    public TimeSpan AcceptedFutureSkew { get; }

    public LiveFreshnessAssessment Assess(DateTime? observedAtUtc, DateTime nowUtc)
    {
        EnsureUtc(nowUtc, nameof(nowUtc));
        if (!observedAtUtc.HasValue)
        {
            return new LiveFreshnessAssessment(
                LiveFreshnessState.Unavailable,
                LiveFreshnessReason.MissingSourceTimestamp,
                null,
                null);
        }

        EnsureUtc(observedAtUtc.Value, nameof(observedAtUtc));
        if (observedAtUtc.Value > nowUtc
            && observedAtUtc.Value - nowUtc > this.AcceptedFutureSkew)
        {
            return new LiveFreshnessAssessment(
                LiveFreshnessState.Unavailable,
                LiveFreshnessReason.SourceTimestampTooFarInFuture,
                null,
                null);
        }

        TimeSpan age = observedAtUtc.Value > nowUtc
            ? TimeSpan.Zero
            : nowUtc - observedAtUtc.Value;
        DateTime latestObservationWithRepresentableExpiration = DateTime.SpecifyKind(
            DateTime.MaxValue.Subtract(this.StaleUntil),
            DateTimeKind.Utc);
        if (observedAtUtc.Value > latestObservationWithRepresentableExpiration)
        {
            throw Invalid(
                LiveDataErrorCodes.InvalidTimestamp,
                "The live observation timestamp is outside the supported freshness range.",
                nameof(observedAtUtc));
        }

        DateTime expiresAtUtc = observedAtUtc.Value.Add(this.StaleUntil);
        if (age <= this.FreshUntil)
        {
            return new LiveFreshnessAssessment(
                LiveFreshnessState.Fresh,
                LiveFreshnessReason.WithinFreshWindow,
                age,
                expiresAtUtc);
        }

        if (age <= this.AgingUntil)
        {
            return new LiveFreshnessAssessment(
                LiveFreshnessState.Aging,
                LiveFreshnessReason.WithinAgingWindow,
                age,
                expiresAtUtc);
        }

        if (age <= this.StaleUntil)
        {
            return new LiveFreshnessAssessment(
                LiveFreshnessState.Stale,
                LiveFreshnessReason.WithinStaleWindow,
                age,
                expiresAtUtc);
        }

        return new LiveFreshnessAssessment(
            LiveFreshnessState.Expired,
            LiveFreshnessReason.ObservationExpired,
            age,
            expiresAtUtc);
    }

    private static string NormalizeVersion(string? value)
    {
        string normalizedValue = value?.Trim() ?? string.Empty;
        if (normalizedValue.Length is 0 or > 100 || normalizedValue.Any(char.IsControl))
        {
            throw Invalid(
                LiveDataErrorCodes.InvalidText,
                "A freshness policy version is required and cannot exceed 100 characters.",
                nameof(value));
        }

        return normalizedValue;
    }

    private static void ValidateThresholds(
        TimeSpan freshUntil,
        TimeSpan agingUntil,
        TimeSpan staleUntil,
        TimeSpan acceptedFutureSkew)
    {
        bool thresholdsAreOrdered = freshUntil > TimeSpan.Zero
            && agingUntil > freshUntil
            && staleUntil > agingUntil
            && staleUntil <= MaximumStaleWindow;
        bool futureSkewIsValid = acceptedFutureSkew >= TimeSpan.Zero
            && acceptedFutureSkew <= MaximumFutureSkew;
        if (!thresholdsAreOrdered || !futureSkewIsValid)
        {
            throw Invalid(
                LiveDataErrorCodes.InvalidFreshnessThresholds,
                "Freshness thresholds must be positive, strictly ordered and within their safety limits.");
        }
    }

    private static void EnsureUtc(DateTime value, string parameterName)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw Invalid(
                LiveDataErrorCodes.InvalidTimestamp,
                "Freshness timestamps must be expressed in UTC.",
                parameterName);
        }
    }

    private static LiveDataValidationException Invalid(
        string code,
        string message,
        string? parameterName = null)
    {
        return new LiveDataValidationException(code, message, parameterName);
    }
}
