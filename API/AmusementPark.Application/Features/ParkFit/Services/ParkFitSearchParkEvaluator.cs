using AmusementPark.Application.Features.ParkFit.Models;
using AmusementPark.Application.Features.ParkFit.Queries;
using AmusementPark.Application.Features.ParkFit.Results;
using AmusementPark.Core.Domain.ParkFit;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Geo;

namespace AmusementPark.Application.Features.ParkFit.Services;

/// <summary>
/// Compose les évaluateurs purs du Core pour un candidat déjà admis par la gate qualité.
/// </summary>
public sealed class ParkFitSearchParkEvaluator
{
    private readonly AttractionCompatibilityEvaluator individualEvaluator =
        new AttractionCompatibilityEvaluator();
    private readonly GroupAttractionCompatibilityEvaluator groupEvaluator =
        new GroupAttractionCompatibilityEvaluator();
    private readonly ParkFitGroupCompatibilitySubscoreEvaluator groupSubscoreEvaluator =
        new ParkFitGroupCompatibilitySubscoreEvaluator();
    private readonly ParkFitPreferenceCoverageSubscoreEvaluator preferenceEvaluator =
        new ParkFitPreferenceCoverageSubscoreEvaluator();
    private readonly ParkFitIndoorResilienceSubscoreEvaluator indoorEvaluator =
        new ParkFitIndoorResilienceSubscoreEvaluator();
    private readonly ParkFitDateAvailabilityEvaluator availabilityEvaluator =
        new ParkFitDateAvailabilityEvaluator();
    private readonly ParkFitTravelConvenienceSubscoreEvaluator travelEvaluator =
        new ParkFitTravelConvenienceSubscoreEvaluator();
    private readonly ParkFitScoreEvaluator scoreEvaluator = new ParkFitScoreEvaluator();

    public ParkFitSearchParkResult Evaluate(
        Park park,
        IReadOnlyCollection<ParkItem> attractions,
        ParkFitDataQualityAssessment quality,
        ParkOpeningHoursSchedule? schedule,
        IReadOnlyCollection<ParkFitEvaluatedMemberProfile> profiles,
        SearchParksByFitQuery query,
        DateTime evaluatedAtUtc,
        int evaluatedHardFilterCount)
    {
        ArgumentNullException.ThrowIfNull(park);
        ArgumentNullException.ThrowIfNull(attractions);
        ArgumentNullException.ThrowIfNull(quality);
        ArgumentNullException.ThrowIfNull(profiles);
        ArgumentNullException.ThrowIfNull(query);

        if (quality.Status != ParkFitDataQualityStatus.EligibleForFitComparison
            || !string.Equals(quality.ParkId, park.Id, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "The evaluated park must match an eligible Park Fit data-quality assessment.",
                nameof(quality));
        }

        List<GroupAttractionCompatibility> groupCompatibilities = attractions
            .Select(attraction => this.EvaluateAttraction(
                attraction,
                profiles,
                query.EvaluationDate,
                evaluatedAtUtc))
            .ToList();
        ParkFitSubscore groupSubscore = this.groupSubscoreEvaluator.Evaluate(
            groupCompatibilities,
            query.EvaluationDate);
        GeoPoint? origin = query.OriginLatitude.HasValue && query.OriginLongitude.HasValue
            ? new GeoPoint(query.OriginLatitude.Value, query.OriginLongitude.Value)
            : null;
        ParkFitTravelEvaluation travel = this.travelEvaluator.Evaluate(
            origin,
            park.Position,
            evaluatedAtUtc);
        IReadOnlyCollection<ParkFitSubscore> subscores = new[]
        {
            groupSubscore,
            this.preferenceEvaluator.Evaluate(query.PreferredAttractionTypes, attractions),
            travel.Subscore,
            this.indoorEvaluator.Evaluate(query.PreferIndoor, attractions),
            BuildNotApplicable(ParkFitSubscoreKind.BudgetFit),
        };
        ParkFitDateAvailability availability = this.availabilityEvaluator.Evaluate(
            schedule,
            query.EvaluationDate);
        ParkFitScore score = this.scoreEvaluator.Evaluate(
            subscores,
            new ParkFitHardFilterEvaluation(
                query.EvaluationDate,
                evaluatedHardFilterCount,
                0,
                0),
            availability,
            query.UnknownDataPolicy,
            query.EvaluationDate,
            evaluatedAtUtc);

        return new ParkFitSearchParkResult
        {
            Park = park,
            DataQuality = quality,
            Score = score,
            DateAvailability = availability,
            TravelDistance = travel.Distance,
            EveryoneTogetherAttractionCount = CountState(
                groupCompatibilities,
                GroupAttractionCompatibilityState.EveryoneTogether),
            SplitRequiredAttractionCount = CountState(
                groupCompatibilities,
                GroupAttractionCompatibilityState.PossibleWithSplit),
            PartialAttractionCount = CountState(
                groupCompatibilities,
                GroupAttractionCompatibilityState.Partial),
            NoCompatibleMemberAttractionCount = CountState(
                groupCompatibilities,
                GroupAttractionCompatibilityState.None),
            UnknownAttractionCount = CountState(
                groupCompatibilities,
                GroupAttractionCompatibilityState.Unknown),
            MemberSummaries = BuildMemberSummaries(profiles, groupCompatibilities),
            CriticalSources = BuildCriticalSources(groupCompatibilities),
        };
    }

    private GroupAttractionCompatibility EvaluateAttraction(
        ParkItem attraction,
        IReadOnlyCollection<ParkFitEvaluatedMemberProfile> profiles,
        DateOnly evaluationDate,
        DateTime evaluatedAtUtc)
    {
        IReadOnlyCollection<AttractionAccessCondition> conditions =
            attraction.AttractionDetails is null
                ? Array.Empty<AttractionAccessCondition>()
                : attraction.AttractionDetails.AccessConditions;
        List<GroupAttractionMemberCompatibility> members = profiles
            .Select(member => new GroupAttractionMemberCompatibility(
                member.MemberKey,
                this.individualEvaluator.Evaluate(
                    member.Profile,
                    conditions,
                    evaluationDate,
                    evaluatedAtUtc,
                    ParkFitSearchLimits.MaximumVerificationAge)))
            .ToList();
        GroupAttractionParticipationConfiguration configuration =
            GroupAttractionParticipationConfigurationResolver.Resolve(members);

        return this.groupEvaluator.Evaluate(members, configuration);
    }

    private static ParkFitSubscore BuildNotApplicable(ParkFitSubscoreKind kind)
    {
        return new ParkFitSubscore(
            kind,
            ParkFitSubscoreState.NotApplicable,
            null,
            0m,
            ParkFitDataConfidence.Unknown);
    }

    private static IReadOnlyCollection<AttractionCompatibilitySourceReference>
        BuildCriticalSources(IEnumerable<GroupAttractionCompatibility> compatibilities)
    {
        return compatibilities
            .SelectMany(static compatibility => compatibility.Members)
            .SelectMany(static member => member.Compatibility.Sources)
            .Distinct(ParkFitCriticalSourceReferenceComparer.Instance)
            .OrderByDescending(static source => source.VerifiedAtUtc)
            .ThenBy(static source => source.Url, StringComparer.Ordinal)
            .ThenBy(static source => source.Reference, StringComparer.Ordinal)
            .Take(ParkFitSearchLimits.MaximumCriticalSourceCountPerPark)
            .ToList();
    }

    private static int CountState(
        IEnumerable<GroupAttractionCompatibility> compatibilities,
        GroupAttractionCompatibilityState state)
    {
        return compatibilities.Count(compatibility => compatibility.State == state);
    }

    private static IReadOnlyCollection<ParkFitSearchMemberSummaryResult> BuildMemberSummaries(
        IReadOnlyCollection<ParkFitEvaluatedMemberProfile> profiles,
        IReadOnlyCollection<GroupAttractionCompatibility> compatibilities)
    {
        Dictionary<string, List<AttractionCompatibilityState>> statesByMember = profiles
            .ToDictionary(
                static profile => profile.MemberKey,
                static _ => new List<AttractionCompatibilityState>(),
                StringComparer.Ordinal);
        foreach (GroupAttractionCompatibility compatibility in compatibilities)
        {
            foreach (GroupAttractionMemberCompatibility member in compatibility.Members)
            {
                if (statesByMember.TryGetValue(
                    member.MemberKey,
                    out List<AttractionCompatibilityState>? states))
                {
                    states.Add(member.Compatibility.State);
                }
            }
        }

        return profiles
            .Select((profile, index) =>
            {
                List<AttractionCompatibilityState> states = statesByMember[profile.MemberKey];

                return new ParkFitSearchMemberSummaryResult
                {
                    MemberNumber = index + 1,
                    CompatibleAloneAttractionCount = states.Count(static state =>
                        state == AttractionCompatibilityState.CompatibleAlone),
                    CompatibleWithCompanionAttractionCount = states.Count(static state =>
                        state == AttractionCompatibilityState.CompatibleWithCompanion),
                    IncompatibleAttractionCount = states.Count(static state =>
                        state == AttractionCompatibilityState.Incompatible),
                    UnknownAttractionCount = states.Count(static state =>
                        state == AttractionCompatibilityState.Unknown),
                    NotApplicableAttractionCount = states.Count(static state =>
                        state == AttractionCompatibilityState.NotApplicable),
                };
            })
            .ToList();
    }
}
