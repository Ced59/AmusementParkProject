namespace AmusementPark.Core.Domain.Parks;

internal sealed class DataCompletenessScoreBuilder
{
    private int earnedPoints;
    private int applicableMaxPoints;
    private string? publicationBlocker;

    public bool HasMissingPoints => this.earnedPoints < this.applicableMaxPoints;

    public void Add(bool isSatisfied, int points)
    {
        if (points <= 0)
        {
            return;
        }

        this.applicableMaxPoints += points;
        if (isSatisfied)
        {
            this.earnedPoints += points;
        }
    }

    public void AddPartial(int earnedParts, int applicableParts, int points)
    {
        if (points <= 0 || applicableParts <= 0)
        {
            return;
        }

        this.applicableMaxPoints += points;
        int normalizedEarnedParts = Math.Clamp(earnedParts, 0, applicableParts);
        this.earnedPoints += (int)Math.Round((double)normalizedEarnedParts / applicableParts * points, MidpointRounding.AwayFromZero);
    }

    public void AddIfApplicable(bool isApplicable, bool isSatisfied, int points)
    {
        if (!isApplicable)
        {
            return;
        }

        this.Add(isSatisfied, points);
    }

    public void AddPublicationBlocker(bool isBlocking, string blockerKey)
    {
        if (!isBlocking || string.IsNullOrWhiteSpace(blockerKey))
        {
            return;
        }

        string normalizedBlockerKey = blockerKey.Trim();
        if (this.publicationBlocker is null
            || string.CompareOrdinal(normalizedBlockerKey, this.publicationBlocker) < 0)
        {
            this.publicationBlocker = normalizedBlockerKey;
        }
    }

    public DataCompletenessScore Build()
    {
        return DataCompletenessScore.FromPoints(this.earnedPoints, this.applicableMaxPoints, this.publicationBlocker);
    }
}
