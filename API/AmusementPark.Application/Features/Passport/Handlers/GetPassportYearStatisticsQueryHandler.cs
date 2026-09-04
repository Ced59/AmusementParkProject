using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Application.Features.Passport.Queries;
using AmusementPark.Application.Features.Passport.Results;
using AmusementPark.Application.Features.Passport.Services;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Handlers;

public sealed class GetPassportYearStatisticsQueryHandler
    : IQueryHandler<
        GetPassportYearStatisticsQuery,
        ApplicationResult<PassportYearStatisticsResult>>
{
    private readonly IPassportScopeStatisticsSourceReader sourceReader;
    private readonly IParkNameReadRepository parkNameReadRepository;

    public GetPassportYearStatisticsQueryHandler(
        IPassportScopeStatisticsSourceReader sourceReader,
        IParkNameReadRepository parkNameReadRepository)
    {
        this.sourceReader = sourceReader;
        this.parkNameReadRepository = parkNameReadRepository;
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
        IReadOnlyDictionary<string, string?> parkNames = parkIds.Length == 0
            ? new Dictionary<string, string?>(StringComparer.Ordinal)
            : await this.parkNameReadRepository.GetNamesByIdsAsync(
                parkIds,
                cancellationToken);
        return ApplicationResult<PassportYearStatisticsResult>.Success(
            PassportStatisticsResultFactory.CreateYear(statistics, parkNames));
    }
}
