namespace AmusementPark.Core.Domain.ParkFit;

public sealed class ParkFitOperationalDecision
{
    public ParkFitOperationalDecision(
        ParkFitOperationalDecisionType type,
        string actorUserId,
        string reason,
        DateTime decidedAtUtc,
        long revision)
    {
        if (!Enum.IsDefined(type)
            || string.IsNullOrWhiteSpace(actorUserId)
            || actorUserId.Trim().Length > 200
            || string.IsNullOrWhiteSpace(reason)
            || reason.Trim().Length > ParkFitOperationalStatus.MaximumReasonLength
            || !IsSafePlainText(reason)
            || decidedAtUtc.Kind != DateTimeKind.Utc
            || revision < 1)
        {
            throw new ArgumentException("The Park Fit operational decision is invalid.");
        }

        this.Type = type;
        this.ActorUserId = actorUserId.Trim();
        this.Reason = reason.Trim();
        this.DecidedAtUtc = decidedAtUtc;
        this.Revision = revision;
    }

    private static bool IsSafePlainText(string value)
    {
        return !value.Any(static character => char.IsControl(character)
                && character is not '\r' and not '\n' and not '\t')
            && !value.Contains('<', StringComparison.Ordinal)
            && !value.Contains('>', StringComparison.Ordinal);
    }

    public ParkFitOperationalDecisionType Type { get; }

    public string ActorUserId { get; }

    public string Reason { get; }

    public DateTime DecidedAtUtc { get; }

    public long Revision { get; }
}
