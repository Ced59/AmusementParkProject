using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Application.Features.Passport.Queries;
using AmusementPark.Application.Features.Passport.Results;
using AmusementPark.Application.Features.Passport.Services;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Handlers;

public sealed class GetPassportParkStatisticsQueryHandler
    : IQueryHandler<
        GetPassportParkStatisticsQuery,
        ApplicationResult<PassportParkStatisticsResult>>
{
    private readonly IPassportScopeStatisticsSourceReader sourceReader;
    private readonly IParkNameReadRepository parkNameReadRepository;
    private readonly IParkItemNameReadRepository parkItemNameReadRepository;

    public GetPassportParkStatisticsQueryHandler(
        IPassportScopeStatisticsSourceReader sourceReader,
        IParkNameReadRepository parkNameReadRepository,
        IParkItemNameReadRepository parkItemNameReadRepository)
    {
        this.sourceReader = sourceReader;
        this.parkNameReadRepository = parkNameReadRepository;
        this.parkItemNameReadRepository = parkItemNameReadRepository;
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
        Task<IReadOnlyDictionary<string, string?>> parkNamesTask = hasPrivateEvidence
            ? this.parkNameReadRepository.GetNamesByIdsAsync(new[] { parkId }, cancellationToken)
            : Task.FromResult<IReadOnlyDictionary<string, string?>>(
                new Dictionary<string, string?>(StringComparer.Ordinal));
        Task<IReadOnlyDictionary<string, string?>> parkItemNamesTask = parkItemIds.Length == 0
            ? Task.FromResult<IReadOnlyDictionary<string, string?>>(
                new Dictionary<string, string?>(StringComparer.Ordinal))
            : this.parkItemNameReadRepository.GetNamesByIdsAsync(parkItemIds, cancellationToken);
        await Task.WhenAll(parkNamesTask, parkItemNamesTask);
        IReadOnlyDictionary<string, string?> parkNames = await parkNamesTask;
        IReadOnlyDictionary<string, string?> parkItemNames = await parkItemNamesTask;
        return ApplicationResult<PassportParkStatisticsResult>.Success(
            PassportStatisticsResultFactory.CreatePark(
                statistics,
                parkNames.GetValueOrDefault(parkId),
                parkItemNames));
    }
}
