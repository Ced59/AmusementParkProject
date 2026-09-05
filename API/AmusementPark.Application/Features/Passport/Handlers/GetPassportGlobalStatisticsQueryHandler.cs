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

public sealed class GetPassportGlobalStatisticsQueryHandler
    : IQueryHandler<
        GetPassportGlobalStatisticsQuery,
        ApplicationResult<PassportGlobalStatisticsResult>>
{
    private readonly IPassportScopeStatisticsSourceReader sourceReader;
    private readonly IParkNameReadRepository parkNameReadRepository;
    private readonly IParkItemNameReadRepository parkItemNameReadRepository;

    public GetPassportGlobalStatisticsQueryHandler(
        IPassportScopeStatisticsSourceReader sourceReader,
        IParkNameReadRepository parkNameReadRepository,
        IParkItemNameReadRepository parkItemNameReadRepository)
    {
        this.sourceReader = sourceReader;
        this.parkNameReadRepository = parkNameReadRepository;
        this.parkItemNameReadRepository = parkItemNameReadRepository;
    }

    public async Task<ApplicationResult<PassportGlobalStatisticsResult>> HandleAsync(
        GetPassportGlobalStatisticsQuery query,
        CancellationToken cancellationToken = default)
    {
        string userId;
        string? parkId = null;
        try
        {
            userId = IdentifierRules.NormalizeRequired(query.UserId, nameof(query.UserId));
            if (query.ParkId is not null)
            {
                parkId = IdentifierRules.NormalizeRequired(query.ParkId, nameof(query.ParkId));
            }
        }
        catch (IdentifierValidationException exception)
        {
            return ApplicationResult<PassportGlobalStatisticsResult>.Failure(
                PassportApplicationErrors.InvalidIdentifier(
                    exception.ErrorCode,
                    exception.Message,
                    exception.ParamName));
        }

        if (query.Year.HasValue
            && (query.Year.Value < DateOnly.MinValue.Year
                || query.Year.Value > DateOnly.MaxValue.Year))
        {
            return ApplicationResult<PassportGlobalStatisticsResult>.Failure(
                PassportApplicationErrors.InvalidYear());
        }

        PassportGlobalStatisticsSource source = await this.sourceReader.ReadGlobalAsync(
            userId,
            query.Year,
            parkId,
            cancellationToken);
        PassportGlobalStatistics statistics = PassportGlobalStatisticsCalculator.Calculate(
            source.Visits,
            source.Rides);
        string[] parkIds = source.AvailableParkIds
            .Concat(statistics.TopItems.Select(static item => item.ParkId))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        string[] parkItemIds = statistics.TopItems
            .Select(static item => item.ParkItemId)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        Task<IReadOnlyDictionary<string, string?>> parkNamesTask = parkIds.Length == 0
            ? Task.FromResult<IReadOnlyDictionary<string, string?>>(
                new Dictionary<string, string?>(StringComparer.Ordinal))
            : this.parkNameReadRepository.GetNamesByIdsAsync(parkIds, cancellationToken);
        Task<IReadOnlyDictionary<string, string?>> parkItemNamesTask = parkItemIds.Length == 0
            ? Task.FromResult<IReadOnlyDictionary<string, string?>>(
                new Dictionary<string, string?>(StringComparer.Ordinal))
            : this.parkItemNameReadRepository.GetNamesByIdsAsync(parkItemIds, cancellationToken);
        await Task.WhenAll(parkNamesTask, parkItemNamesTask);

        return ApplicationResult<PassportGlobalStatisticsResult>.Success(
            PassportGlobalStatisticsResultFactory.Create(
                statistics,
                query.Year,
                parkId,
                source.AvailableYears,
                source.AvailableParkIds,
                await parkNamesTask,
                await parkItemNamesTask));
    }
}
