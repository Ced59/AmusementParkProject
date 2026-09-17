namespace AmusementPark.Core.Domain.Trips;

public sealed class TripFitRecommendationSnapshot
{
    public const int MaximumMethodVersionLength = 50;
    public const int MaximumExplanationLength = 1000;

    public TripFitRecommendationSnapshot(
        string methodVersion,
        string explanation,
        DateTime calculatedAtUtc)
    {
        string normalizedMethodVersion = methodVersion?.Trim() ?? string.Empty;
        string normalizedExplanation = explanation?.Trim() ?? string.Empty;
        if (normalizedMethodVersion.Length is 0 or > MaximumMethodVersionLength)
        {
            throw Invalid("The FIT method version is required and must remain bounded.");
        }

        if (normalizedExplanation.Length is 0 or > MaximumExplanationLength)
        {
            throw Invalid("The FIT explanation is required and must remain bounded.");
        }

        if (calculatedAtUtc.Kind != DateTimeKind.Utc)
        {
            throw Invalid("The FIT calculation timestamp must be expressed in UTC.");
        }

        this.MethodVersion = normalizedMethodVersion;
        this.Explanation = normalizedExplanation;
        this.CalculatedAtUtc = calculatedAtUtc;
    }

    public string MethodVersion { get; }

    public string Explanation { get; }

    public DateTime CalculatedAtUtc { get; }

    private static TripPlanValidationException Invalid(string message)
    {
        return new TripPlanValidationException(TripPlanErrorCodes.InvalidCandidate, message);
    }
}
