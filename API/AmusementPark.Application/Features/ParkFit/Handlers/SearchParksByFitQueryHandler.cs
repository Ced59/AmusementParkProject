using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkFit.Models;
using AmusementPark.Application.Features.ParkFit.Queries;
using AmusementPark.Application.Features.ParkFit.Results;
using AmusementPark.Application.Features.ParkFit.Services;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkOpeningHours.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Validation;
using AmusementPark.Core.Domain.ParkFit;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkFit.Handlers;

/// <summary>
/// Orchestre une recherche FIT anonyme avec des lectures groupées et bornées.
/// </summary>
public sealed class SearchParksByFitQueryHandler
    : IQueryHandler<SearchParksByFitQuery, ApplicationResult<ParkFitSearchResult>>
{
    private readonly IParkRepository parkRepository;
    private readonly IParkItemRepository parkItemRepository;
    private readonly IParkOpeningHoursRepository openingHoursRepository;
    private readonly IApplicationValidator<SearchParksByFitQuery> validator;
    private readonly ParkFitSearchParkEvaluator parkEvaluator;
    private readonly TimeProvider timeProvider;
    private readonly ParkFitDataQualityAssessor qualityAssessor =
        new ParkFitDataQualityAssessor();

    public SearchParksByFitQueryHandler(
        IParkRepository parkRepository,
        IParkItemRepository parkItemRepository,
        IParkOpeningHoursRepository openingHoursRepository,
        IApplicationValidator<SearchParksByFitQuery> validator,
        ParkFitSearchParkEvaluator parkEvaluator,
        TimeProvider? timeProvider = null)
    {
        this.parkRepository = parkRepository;
        this.parkItemRepository = parkItemRepository;
        this.openingHoursRepository = openingHoursRepository;
        this.validator = validator;
        this.parkEvaluator = parkEvaluator;
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<ApplicationResult<ParkFitSearchResult>> HandleAsync(
        SearchParksByFitQuery query,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<ApplicationError> errors = this.validator.Validate(query);
        if (errors.Count > 0)
        {
            return ApplicationResult<ParkFitSearchResult>.Failure(errors);
        }

        string? countryCode = string.IsNullOrWhiteSpace(query.CountryCode)
            ? null
            : query.CountryCode.Trim().ToUpperInvariant();
        PagedResult<Park> candidatePage = await this.parkRepository.GetPageAsync(
            1,
            ParkFitSearchLimits.MaximumInspectedCandidateCount,
            includeHidden: false,
            isVisible: true,
            adminReviewStatus: null,
            type: null,
            countryCode: countryCode,
            hasValidCoordinates: true,
            closedFilter: ClosedEntityFilter.OpenOnly,
            cancellationToken: cancellationToken,
            sortField: ParkAdminSortField.Name);
        List<string> parkIds = candidatePage.Items
            .Select(static park => park.Id)
            .Where(static parkId => !string.IsNullOrWhiteSpace(parkId))
            .Distinct(StringComparer.Ordinal)
            .ToList();
        DateTime evaluatedAtUtc = this.timeProvider.GetUtcNow().UtcDateTime;

        if (parkIds.Count == 0)
        {
            return ApplicationResult<ParkFitSearchResult>.Success(new ParkFitSearchResult
            {
                MethodVersion = ParkFitScoreEvaluator.MethodVersion,
                EvaluationDate = query.EvaluationDate,
                EvaluatedAtUtc = evaluatedAtUtc,
                TotalCandidateCount = candidatePage.TotalItems,
                CandidatePoolTruncated = candidatePage.TotalItems > 0,
            });
        }

        Task<IReadOnlyCollection<ParkItem>> itemsTask =
            this.parkItemRepository.GetVisibleOpenAttractionsByParkIdsAsync(
                parkIds,
                cancellationToken);
        Task<IReadOnlyDictionary<string, ParkOpeningHoursScheduleSummary>> summariesTask =
            this.openingHoursRepository.GetSummariesByParkIdsAsync(parkIds, cancellationToken);
        await Task.WhenAll(itemsTask, summariesTask);

        IReadOnlyDictionary<string, IReadOnlyCollection<ParkItem>> itemsByParkId =
            (await itemsTask)
                .GroupBy(static item => item.ParkId, StringComparer.Ordinal)
                .ToDictionary(
                    static group => group.Key,
                    static group => (IReadOnlyCollection<ParkItem>)group.ToList(),
                    StringComparer.Ordinal);
        IReadOnlyDictionary<string, ParkOpeningHoursScheduleSummary> summariesByParkId =
            await summariesTask;
        IReadOnlyCollection<ParkFitEvaluatedMemberProfile> profiles =
            BuildProfiles(query.Members);
        List<(
            Park Park,
            IReadOnlyCollection<ParkItem> Attractions,
            ParkFitDataQualityAssessment Quality)> eligibleCandidates = new();
        Dictionary<ParkFitDataQualityStatus, int> qualityStatusCounts =
            new Dictionary<ParkFitDataQualityStatus, int>();
        Dictionary<ParkFitDataQualityIssue, int> qualityIssueCounts =
            new Dictionary<ParkFitDataQualityIssue, int>();

        foreach (Park park in candidatePage.Items)
        {
            IReadOnlyCollection<ParkItem> attractions = itemsByParkId.TryGetValue(
                park.Id,
                out IReadOnlyCollection<ParkItem>? parkItems)
                ? parkItems
                : Array.Empty<ParkItem>();
            ParkFitDataQualityAssessment quality = this.qualityAssessor.Assess(
                park,
                attractions,
                summariesByParkId.TryGetValue(
                    park.Id,
                    out ParkOpeningHoursScheduleSummary? summary)
                    ? summary
                    : null,
                evaluatedAtUtc,
                ParkFitSearchLimits.MaximumVerificationAge,
                applicabilityDate: query.EvaluationDate);
            qualityStatusCounts[quality.Status] = qualityStatusCounts.GetValueOrDefault(
                quality.Status) + 1;
            if (quality.Status != ParkFitDataQualityStatus.EligibleForFitComparison)
            {
                foreach (ParkFitDataQualityIssue issue in quality.Issues.Distinct())
                {
                    qualityIssueCounts[issue] = qualityIssueCounts.GetValueOrDefault(issue) + 1;
                }

                continue;
            }

            eligibleCandidates.Add((park, attractions, quality));
        }

        IReadOnlyCollection<string> eligibleParkIds = eligibleCandidates
            .Select(static candidate => candidate.Park.Id)
            .ToList();
        IReadOnlyDictionary<string, ParkOpeningHoursSchedule> schedulesByParkId =
            eligibleParkIds.Count == 0
                ? new Dictionary<string, ParkOpeningHoursSchedule>(StringComparer.Ordinal)
                : (await this.openingHoursRepository.GetByParkIdsAsync(
                    eligibleParkIds,
                    cancellationToken))
                    .Where(static schedule => !string.IsNullOrWhiteSpace(schedule.ParkId))
                    .GroupBy(static schedule => schedule.ParkId, StringComparer.Ordinal)
                    .ToDictionary(
                        static group => group.Key,
                        static group => group.First(),
                        StringComparer.Ordinal);
        List<ParkFitSearchParkResult> eligibleResults = eligibleCandidates
            .Select(candidate => this.parkEvaluator.Evaluate(
                candidate.Park,
                candidate.Attractions,
                candidate.Quality,
                schedulesByParkId.TryGetValue(
                    candidate.Park.Id,
                    out ParkOpeningHoursSchedule? schedule)
                    ? schedule
                    : null,
                profiles,
                query,
                evaluatedAtUtc,
                countryCode is null ? 0 : 1))
            .ToList();

        IReadOnlyCollection<ParkFitSearchParkResult> orderedResults = eligibleResults
            .OrderBy(static result => GetStateOrder(result.Score.State))
            .ThenByDescending(static result => result.Score.ComparativeScore)
            .ThenByDescending(static result => result.Score.RawKnownScore)
            .ThenByDescending(static result => result.Score.CoveragePercent)
            .ThenBy(static result => result.Park.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static result => result.Park.Id, StringComparer.Ordinal)
            .Take(query.MaximumResults)
            .ToList();
        int eligibleCandidateCount = qualityStatusCounts.GetValueOrDefault(
            ParkFitDataQualityStatus.EligibleForFitComparison);

        return ApplicationResult<ParkFitSearchResult>.Success(new ParkFitSearchResult
        {
            MethodVersion = ParkFitScoreEvaluator.MethodVersion,
            EvaluationDate = query.EvaluationDate,
            EvaluatedAtUtc = evaluatedAtUtc,
            TotalCandidateCount = candidatePage.TotalItems,
            InspectedCandidateCount = candidatePage.Items.Count,
            QualityEligibleCandidateCount = eligibleCandidateCount,
            QualityRejectedCandidateCount = candidatePage.Items.Count - eligibleCandidateCount,
            CandidatePoolTruncated = candidatePage.TotalItems > candidatePage.Items.Count,
            QualityStatusCounts = qualityStatusCounts,
            QualityIssueCounts = qualityIssueCounts,
            Parks = orderedResults,
        });
    }

    private static IReadOnlyCollection<ParkFitEvaluatedMemberProfile>
        BuildProfiles(IEnumerable<ParkFitSearchMemberCriteria> members)
    {
        return members
            .Select(member => new ParkFitEvaluatedMemberProfile(
                member.MemberKey.Trim(),
                new ParkFitMemberProfile(
                    member.HeightCentimeters,
                    BuildAgeRange(member.MinimumAgeYears, member.MaximumAgeYears),
                    member.CanBeAccompanied,
                    BuildAgeRange(
                        member.CompanionMinimumAgeYears,
                        member.CompanionMaximumAgeYears))))
            .ToList();
    }

    private static ParkFitAgeRange? BuildAgeRange(int? minimumYears, int? maximumYears)
    {
        return minimumYears.HasValue && maximumYears.HasValue
            ? new ParkFitAgeRange(minimumYears.Value, maximumYears.Value)
            : null;
    }

    private static int GetStateOrder(ParkFitScoreState state)
    {
        return state switch
        {
            ParkFitScoreState.Available => 0,
            ParkFitScoreState.Capped => 1,
            ParkFitScoreState.Suspended => 2,
            ParkFitScoreState.Excluded => 3,
            _ => throw new ArgumentOutOfRangeException(nameof(state)),
        };
    }
}
