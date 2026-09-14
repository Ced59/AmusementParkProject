namespace AmusementPark.Core.Domain.ParkFit;

/// <summary>
/// Résultat de groupe explicable qui conserve chaque verdict individuel.
/// </summary>
public sealed class GroupAttractionCompatibility
{
    internal GroupAttractionCompatibility(
        string methodVersion,
        GroupAttractionCompatibilityState state,
        GroupAttractionParticipationConfiguration participationConfiguration,
        IReadOnlyCollection<GroupAttractionMemberCompatibility> members,
        IReadOnlyCollection<GroupAttractionCompatibilityReasonCode> reasons,
        ParkFitDataConfidence confidence,
        DateOnly evaluationDate,
        DateTime evaluatedAtUtc,
        int compatibleAloneCount,
        int compatibleWithCompanionCount,
        int incompatibleCount,
        int unknownCount,
        int notApplicableCount)
    {
        this.MethodVersion = methodVersion;
        this.State = state;
        this.ParticipationConfiguration = participationConfiguration;
        this.Members = members.ToList();
        this.Reasons = reasons.ToList();
        this.Confidence = confidence;
        this.EvaluationDate = evaluationDate;
        this.EvaluatedAtUtc = evaluatedAtUtc;
        this.CompatibleAloneCount = compatibleAloneCount;
        this.CompatibleWithCompanionCount = compatibleWithCompanionCount;
        this.IncompatibleCount = incompatibleCount;
        this.UnknownCount = unknownCount;
        this.NotApplicableCount = notApplicableCount;
    }

    public string MethodVersion { get; }

    public GroupAttractionCompatibilityState State { get; }

    public GroupAttractionParticipationConfiguration ParticipationConfiguration { get; }

    public IReadOnlyCollection<GroupAttractionMemberCompatibility> Members { get; }

    public IReadOnlyCollection<GroupAttractionCompatibilityReasonCode> Reasons { get; }

    public ParkFitDataConfidence Confidence { get; }

    public DateOnly EvaluationDate { get; }

    public DateTime EvaluatedAtUtc { get; }

    public int CompatibleAloneCount { get; }

    public int CompatibleWithCompanionCount { get; }

    public int IncompatibleCount { get; }

    public int UnknownCount { get; }

    public int NotApplicableCount { get; }
}
