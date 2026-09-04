using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Application.Features.Passport.Queries;
using AmusementPark.Application.Features.Passport.Results;
using AmusementPark.Application.Features.Passport.Services;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Handlers;

public sealed class GetPassportParkStatisticsQueryHandler
    : IQueryHandler<
        GetPassportParkStatisticsQuery,
        ApplicationResult<PassportParkStatisticsResult>>
{
    private readonly IPassportScopeStatisticsSourceReader sourceReader;
    private readonly IParkRepository parkRepository;
    private readonly IParkItemRepository parkItemRepository;

    public GetPassportParkStatisticsQueryHandler(
        IPassportScopeStatisticsSourceReader sourceReader,
        IParkRepository parkRepository,
        IParkItemRepository parkItemRepository)
    {
        this.sourceReader = sourceReader;
        this.parkRepository = parkRepository;
        this.parkItemRepository = parkItemRepository;
    }

    public async Task<ApplicationResult<PassportParkStatisticsResult>> HandleAsync(
        GetPassportParkStatisticsQuery query,
        CancellationToken cancellationToken = default)
    {
        string userId;
        string parkId;
        try
        {
            userId = IdentifierRules.NormalizeRequired(query.UserId, nameof(query.UserId));
            parkId = IdentifierRules.NormalizeRequired(query.ParkId, nameof(query.ParkId));
        }
        catch (IdentifierValidationException exception)
        {
            return ApplicationResult<PassportParkStatisticsResult>.Failure(
                PassportApplicationErrors.InvalidIdentifier(
                    exception.ErrorCode,
                    exception.Message,
                    exception.ParamName));
        }

        PassportParkStatisticsSource source = await this.sourceReader.ReadParkAsync(
            userId,
            parkId,
            cancellationToken);
        PassportParkStatistics statistics = PassportScopeStatisticsCalculator.CalculatePark(
            parkId,
            source.Visits,
            source.Rides,
            source.CurrentGlobalRating,
            source.CurrentItemRatings);
        string[] parkItemIds = statistics.CurrentTopItems
            .Select(static item => item.ParkItemId)
            .Concat(statistics.HistoricalTopItems.Select(static item => item.ParkItemId))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        bool hasPrivateEvidence = statistics.Summary.VisitCount > 0
            || statistics.CurrentGlobalRating.HasValue
            || statistics.CurrentTopItems.Count > 0
            || statistics.HistoricalTopItems.Count > 0;
        Task<Park?> parkTask = hasPrivateEvidence
            ? this.parkRepository.GetByIdAsync(parkId, true, cancellationToken)
            : Task.FromResult<Park?>(null);
        Task<IReadOnlyCollection<ParkItem>> parkItemsTask = parkItemIds.Length == 0
            ? Task.FromResult<IReadOnlyCollection<ParkItem>>(Array.Empty<ParkItem>())
            : this.parkItemRepository.GetByIdsAsync(parkItemIds, cancellationToken);
        await Task.WhenAll(parkTask, parkItemsTask);
        IReadOnlyDictionary<string, string> parkItemNames = (await parkItemsTask)
            .Where(static item => !string.IsNullOrWhiteSpace(item.Id)
                && !string.IsNullOrWhiteSpace(item.Name))
            .GroupBy(static item => item.Id, StringComparer.Ordinal)
            .ToDictionary(
                static group => group.Key,
                static group => group.First().Name,
                StringComparer.Ordinal);
        return ApplicationResult<PassportParkStatisticsResult>.Success(
            PassportStatisticsResultFactory.CreatePark(
                statistics,
                (await parkTask)?.Name,
                parkItemNames));
    }
}
