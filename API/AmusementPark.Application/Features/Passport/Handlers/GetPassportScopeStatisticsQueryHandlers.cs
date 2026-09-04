using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Application.Features.Passport.Queries;
using AmusementPark.Application.Features.Passport.Results;
using AmusementPark.Application.Features.Passport.Services;
using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Handlers;

public sealed class GetPassportParkStatisticsQueryHandler
    : IQueryHandler<
        GetPassportParkStatisticsQuery,
        ApplicationResult<PassportParkStatisticsResult>>
{
    private readonly IPassportScopeStatisticsSourceReader sourceReader;

    public GetPassportParkStatisticsQueryHandler(
        IPassportScopeStatisticsSourceReader sourceReader)
    {
        this.sourceReader = sourceReader;
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
        return ApplicationResult<PassportParkStatisticsResult>.Success(
            PassportStatisticsResultFactory.CreatePark(statistics));
    }
}

public sealed class GetPassportYearStatisticsQueryHandler
    : IQueryHandler<
        GetPassportYearStatisticsQuery,
        ApplicationResult<PassportYearStatisticsResult>>
{
    private readonly IPassportScopeStatisticsSourceReader sourceReader;

    public GetPassportYearStatisticsQueryHandler(
        IPassportScopeStatisticsSourceReader sourceReader)
    {
        this.sourceReader = sourceReader;
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
        return ApplicationResult<PassportYearStatisticsResult>.Success(
            PassportStatisticsResultFactory.CreateYear(statistics));
    }
}
