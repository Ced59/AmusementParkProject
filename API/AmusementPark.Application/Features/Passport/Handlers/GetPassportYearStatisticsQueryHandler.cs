using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Application.Features.Passport.Queries;
using AmusementPark.Application.Features.Passport.Results;
using AmusementPark.Application.Features.Passport.Services;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Handlers;

public sealed class GetPassportYearStatisticsQueryHandler
    : IQueryHandler<
        GetPassportYearStatisticsQuery,
        ApplicationResult<PassportYearStatisticsResult>>
{
    private readonly IPassportScopeStatisticsSourceReader sourceReader;
    private readonly IParkRepository parkRepository;

    public GetPassportYearStatisticsQueryHandler(
        IPassportScopeStatisticsSourceReader sourceReader,
        IParkRepository parkRepository)
    {
        this.sourceReader = sourceReader;
        this.parkRepository = parkRepository;
    }

    public async Task<ApplicationResult<PassportYearStatisticsResult>> HandleAsync(
        GetPassportYearStatisticsQuery query,
        CancellationToken cancellationToken = default)
    {
        string userId;
        try
        {
            userId = IdentifierRules.NormalizeRequired(query.UserId, nameof(query.UserId));
        }
        catch (IdentifierValidationException exception)
        {
            return ApplicationResult<PassportYearStatisticsResult>.Failure(
                PassportApplicationErrors.InvalidIdentifier(
                    exception.ErrorCode,
                    exception.Message,
                    exception.ParamName));
        }

        if (query.Year < DateOnly.MinValue.Year || query.Year > DateOnly.MaxValue.Year)
        {
            return ApplicationResult<PassportYearStatisticsResult>.Failure(
                PassportApplicationErrors.InvalidYear());
        }

        PassportYearStatisticsSource source = await this.sourceReader.ReadYearAsync(
            userId,
            query.Year,
            cancellationToken);
        PassportYearStatistics statistics = PassportScopeStatisticsCalculator.CalculateYear(
            query.Year,
            source.Visits,
            source.Rides);
        string[] parkIds = statistics.ByPark
            .Select(static item => item.ParkId)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        IReadOnlyCollection<Park> parks = parkIds.Length == 0
            ? Array.Empty<Park>()
            : await this.parkRepository.GetByIdsAsync(parkIds, cancellationToken);
        IReadOnlyDictionary<string, string> parkNames = parks
            .Where(static park => !string.IsNullOrWhiteSpace(park.Id)
                && !string.IsNullOrWhiteSpace(park.Name))
            .GroupBy(static park => park.Id, StringComparer.Ordinal)
            .ToDictionary(
                static group => group.Key,
                static group => group.First().Name!,
                StringComparer.Ordinal);
        return ApplicationResult<PassportYearStatisticsResult>.Success(
            PassportStatisticsResultFactory.CreateYear(statistics, parkNames));
    }
}
