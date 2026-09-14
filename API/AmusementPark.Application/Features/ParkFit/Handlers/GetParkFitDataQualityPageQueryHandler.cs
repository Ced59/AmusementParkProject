using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkFit.Queries;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkOpeningHours.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Validation;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkFit.Handlers;

/// <summary>
/// Orchestre l'audit FIT sans dupliquer les règles du Core.
/// </summary>
public sealed class GetParkFitDataQualityPageQueryHandler
    : IQueryHandler<
        GetParkFitDataQualityPageQuery,
        ApplicationResult<PagedResult<ParkFitDataQualityAssessment>>>
{
    public static readonly TimeSpan MaximumVerificationAge = TimeSpan.FromDays(365);

    private readonly IParkRepository parkRepository;
    private readonly IParkItemRepository parkItemRepository;
    private readonly IParkOpeningHoursRepository openingHoursRepository;
    private readonly PagedQueryValidator pagingValidator;
    private readonly ParkFitDataQualityAssessor assessor;
    private readonly TimeProvider timeProvider;

    public GetParkFitDataQualityPageQueryHandler(
        IParkRepository parkRepository,
        IParkItemRepository parkItemRepository,
        IParkOpeningHoursRepository openingHoursRepository,
        PagedQueryValidator pagingValidator,
        ParkFitDataQualityAssessor? assessor = null,
        TimeProvider? timeProvider = null)
    {
        this.parkRepository = parkRepository;
        this.parkItemRepository = parkItemRepository;
        this.openingHoursRepository = openingHoursRepository;
        this.pagingValidator = pagingValidator;
        this.assessor = assessor ?? new ParkFitDataQualityAssessor();
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<ApplicationResult<PagedResult<ParkFitDataQualityAssessment>>> HandleAsync(
        GetParkFitDataQualityPageQuery query,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<ApplicationError> errors = this.pagingValidator.Validate(query.Paging);
        if (errors.Count > 0)
        {
            return ApplicationResult<PagedResult<ParkFitDataQualityAssessment>>.Failure(errors);
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
            return ApplicationResult<PagedResult<ParkFitDataQualityAssessment>>.Success(
                new PagedResult<ParkFitDataQualityAssessment>(
                    Array.Empty<ParkFitDataQualityAssessment>(),
                    parks.Page,
                    parks.PageSize,
                    parks.TotalItems));
        }

        Task<IReadOnlyCollection<ParkItem>> itemsTask =
            this.parkItemRepository.GetByParkIdsAsync(parkIds, true, cancellationToken);
        Task<IReadOnlyDictionary<string, ParkOpeningHoursScheduleSummary>> openingHoursTask =
            this.openingHoursRepository.GetSummariesByParkIdsAsync(parkIds, cancellationToken);
        await Task.WhenAll(itemsTask, openingHoursTask);

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
        List<ParkFitDataQualityAssessment> assessments = parks.Items
            .Select(park => this.assessor.Assess(
                park,
                itemsByParkId.TryGetValue(park.Id, out IReadOnlyCollection<ParkItem>? items)
                    ? items
                    : Array.Empty<ParkItem>(),
                openingHours.TryGetValue(park.Id, out ParkOpeningHoursScheduleSummary? summary)
                    ? summary
                    : null,
                evaluatedAtUtc,
                MaximumVerificationAge))
            .ToList();

        return ApplicationResult<PagedResult<ParkFitDataQualityAssessment>>.Success(
            new PagedResult<ParkFitDataQualityAssessment>(
                assessments,
                parks.Page,
                parks.PageSize,
                parks.TotalItems));
    }
}
