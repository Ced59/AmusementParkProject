using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkFit.Queries;
using AmusementPark.Application.Features.ParkFit.Ports;
using AmusementPark.Application.Features.ParkFit.Results;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkOpeningHours.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Validation;
using AmusementPark.Core.Domain.ParkFit;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkFit.Handlers;

/// <summary>
/// Orchestre l'audit FIT sans dupliquer les règles du Core.
/// </summary>
public sealed class GetParkFitDataQualityPageQueryHandler
    : IQueryHandler<
        GetParkFitDataQualityPageQuery,
        ApplicationResult<PagedResult<ParkFitDataQualityOperationsResult>>>
{
    public static readonly TimeSpan MaximumVerificationAge = TimeSpan.FromDays(365);

    private readonly IParkRepository parkRepository;
    private readonly IParkItemRepository parkItemRepository;
    private readonly IParkOpeningHoursRepository openingHoursRepository;
    private readonly IParkFitOperationalStatusRepository operationalStatusRepository;
    private readonly IParkFitSourceReportRepository sourceReportRepository;
    private readonly PagedQueryValidator pagingValidator;
    private readonly ParkFitDataQualityAssessor assessor;
    private readonly TimeProvider timeProvider;

    public GetParkFitDataQualityPageQueryHandler(
        IParkRepository parkRepository,
        IParkItemRepository parkItemRepository,
        IParkOpeningHoursRepository openingHoursRepository,
        IParkFitOperationalStatusRepository operationalStatusRepository,
        IParkFitSourceReportRepository sourceReportRepository,
        PagedQueryValidator pagingValidator,
        ParkFitDataQualityAssessor? assessor = null,
        TimeProvider? timeProvider = null)
    {
        this.parkRepository = parkRepository;
        this.parkItemRepository = parkItemRepository;
        this.openingHoursRepository = openingHoursRepository;
        this.operationalStatusRepository = operationalStatusRepository;
        this.sourceReportRepository = sourceReportRepository;
        this.pagingValidator = pagingValidator;
        this.assessor = assessor ?? new ParkFitDataQualityAssessor();
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<ApplicationResult<PagedResult<ParkFitDataQualityOperationsResult>>> HandleAsync(
        GetParkFitDataQualityPageQuery query,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<ApplicationError> errors = this.pagingValidator.Validate(query.Paging);
        if (errors.Count > 0)
        {
            return ApplicationResult<PagedResult<ParkFitDataQualityOperationsResult>>.Failure(errors);
        }

        PagedResult<Park> parks = await this.parkRepository.GetPageAsync(
            query.Paging.Page,
            query.Paging.PageSize,
            includeHidden: true,
            isVisible: null,
            adminReviewStatus: null,
            type: null,
            countryCode: null,
            hasValidCoordinates: null,
            closedFilter: ClosedEntityFilter.OpenOnly,
            cancellationToken: cancellationToken,
            sortField: ParkAdminSortField.Name);
        List<string> parkIds = parks.Items
            .Select(static park => park.Id)
            .Where(static parkId => !string.IsNullOrWhiteSpace(parkId))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (parkIds.Count == 0)
        {
            return ApplicationResult<PagedResult<ParkFitDataQualityOperationsResult>>.Success(
                new PagedResult<ParkFitDataQualityOperationsResult>(
                    Array.Empty<ParkFitDataQualityOperationsResult>(),
                    parks.Page,
                    parks.PageSize,
                    parks.TotalItems));
        }

        Task<IReadOnlyCollection<ParkItem>> itemsTask =
            this.parkItemRepository.GetVisibleOpenAttractionsByParkIdsAsync(
                parkIds,
                cancellationToken);
        Task<IReadOnlyDictionary<string, ParkOpeningHoursScheduleSummary>> openingHoursTask =
            this.openingHoursRepository.GetSummariesByParkIdsAsync(parkIds, cancellationToken);
        Task<IReadOnlyDictionary<string, ParkFitOperationalStatus>> operationalStatusesTask =
            this.operationalStatusRepository.GetByParkIdsAsync(parkIds, cancellationToken);
        Task<IReadOnlyDictionary<string, int>> pendingReportsTask =
            this.sourceReportRepository.CountPendingByParkIdsAsync(parkIds, cancellationToken);
        await Task.WhenAll(
            itemsTask,
            openingHoursTask,
            operationalStatusesTask,
            pendingReportsTask);

        IReadOnlyCollection<ParkItem> parkItems = await itemsTask;
        IReadOnlyDictionary<string, ParkOpeningHoursScheduleSummary> openingHours = await openingHoursTask;
        IReadOnlyDictionary<string, IReadOnlyCollection<ParkItem>> itemsByParkId = parkItems
            .Where(static item => !string.IsNullOrWhiteSpace(item.ParkId))
            .GroupBy(static item => item.ParkId, StringComparer.Ordinal)
            .ToDictionary(
                static group => group.Key,
                static group => (IReadOnlyCollection<ParkItem>)group.ToList(),
                StringComparer.Ordinal);
        DateTime evaluatedAtUtc = this.timeProvider.GetUtcNow().UtcDateTime;
        IReadOnlyDictionary<string, ParkFitOperationalStatus> operationalStatuses =
            await operationalStatusesTask;
        IReadOnlyDictionary<string, int> pendingReports = await pendingReportsTask;
        List<ParkFitDataQualityOperationsResult> assessments = parks.Items
            .Select(park => BuildResult(
                this.assessor.Assess(
                    park,
                    itemsByParkId.TryGetValue(park.Id, out IReadOnlyCollection<ParkItem>? items)
                        ? items
                        : Array.Empty<ParkItem>(),
                    openingHours.TryGetValue(park.Id, out ParkOpeningHoursScheduleSummary? summary)
                        ? summary
                        : null,
                    evaluatedAtUtc,
                    MaximumVerificationAge),
                operationalStatuses.GetValueOrDefault(park.Id),
                pendingReports.GetValueOrDefault(park.Id)))
            .ToList();

        return ApplicationResult<PagedResult<ParkFitDataQualityOperationsResult>>.Success(
            new PagedResult<ParkFitDataQualityOperationsResult>(
                assessments,
                parks.Page,
                parks.PageSize,
                parks.TotalItems));
    }

    private static ParkFitDataQualityOperationsResult BuildResult(
        ParkFitDataQualityAssessment assessment,
        ParkFitOperationalStatus? operationalStatus,
        int pendingReportCount)
    {
        ParkFitOperationalStatus status = operationalStatus
            ?? ParkFitOperationalStatus.CreateActive(assessment.ParkId);
        return new ParkFitDataQualityOperationsResult
        {
            Assessment = assessment,
            RecommendationState = status.State,
            OperationalRevision = status.Revision,
            OperationalUpdatedAtUtc = status.UpdatedAtUtc,
            PendingReportCount = pendingReportCount,
            RecentDecisions = status.Decisions
                .OrderByDescending(static decision => decision.Revision)
                .Take(5)
                .Select(static decision => new ParkFitOperationalDecisionResult(
                    decision.Type,
                    decision.Reason,
                    decision.DecidedAtUtc,
                    decision.Revision))
                .ToList(),
        };
    }
}
