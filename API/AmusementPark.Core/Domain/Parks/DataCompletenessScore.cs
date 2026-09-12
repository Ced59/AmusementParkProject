namespace AmusementPark.Core.Domain.Parks;

public sealed record DataCompletenessScore
{
    private const int PublicationBlockerScoreCeiling = 95;

    public int CompletenessScore { get; init; }

    public DataQualityLevel DataQualityLevel { get; init; }

    public int ApplicableMaxPoints { get; init; }

    public int EarnedPoints { get; init; }

    public string? PublicationBlocker { get; init; }

    public static DataCompletenessScore FromPoints(
        int earnedPoints,
        int applicableMaxPoints,
        string? publicationBlocker = null)
    {
        int normalizedApplicableMaxPoints = Math.Max(0, applicableMaxPoints);
        int normalizedEarnedPoints = Math.Clamp(earnedPoints, 0, normalizedApplicableMaxPoints);
        string? normalizedPublicationBlocker = string.IsNullOrWhiteSpace(publicationBlocker)
            ? null
            : publicationBlocker.Trim();
        int completenessScore = normalizedApplicableMaxPoints == 0
            ? 0
            : (int)Math.Round((double)normalizedEarnedPoints / normalizedApplicableMaxPoints * 100d, MidpointRounding.AwayFromZero);

        completenessScore = Math.Clamp(completenessScore, 0, 100);
        if (normalizedPublicationBlocker is not null)
        {
            completenessScore = Math.Min(completenessScore, PublicationBlockerScoreCeiling);
        }

        return new DataCompletenessScore
        {
            CompletenessScore = completenessScore,
            DataQualityLevel = ResolveLevel(completenessScore),
            ApplicableMaxPoints = normalizedApplicableMaxPoints,
            EarnedPoints = normalizedEarnedPoints,
            PublicationBlocker = normalizedPublicationBlocker,
        };
    }

    private static DataQualityLevel ResolveLevel(int completenessScore)
    {
        if (completenessScore >= 95)
        {
            return DataQualityLevel.Excellent;
        }

        if (completenessScore >= 85)
        {
            return DataQualityLevel.Good;
        }

        if (completenessScore >= 70)
        {
            return DataQualityLevel.Publishable;
        }

        if (completenessScore >= 50)
        {
            return DataQualityLevel.Partial;
        }

        if (completenessScore >= 30)
        {
            return DataQualityLevel.Weak;
        }

        return DataQualityLevel.Critical;
    }
}
