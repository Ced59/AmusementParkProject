using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Application.Features.Passport.Queries;
using AmusementPark.Application.Features.Passport.Results;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Handlers;

public sealed class GetPassportItemStatisticsQueryHandler
    : IQueryHandler<
        GetPassportItemStatisticsQuery,
        ApplicationResult<PassportItemStatisticsResult>>
{
    private readonly IPassportItemStatisticsSourceReader sourceReader;
    private readonly IRatingRepository ratingRepository;

    public GetPassportItemStatisticsQueryHandler(
        IPassportItemStatisticsSourceReader sourceReader,
        IRatingRepository ratingRepository)
    {
        this.sourceReader = sourceReader;
        this.ratingRepository = ratingRepository;
    }

    public async Task<ApplicationResult<PassportItemStatisticsResult>> HandleAsync(
        GetPassportItemStatisticsQuery query,
        CancellationToken cancellationToken = default)
    {
        string userId;
        string parkItemId;
        try
        {
            userId = IdentifierRules.NormalizeRequired(query.UserId, nameof(query.UserId));
            parkItemId = IdentifierRules.NormalizeRequired(
                query.ParkItemId,
                nameof(query.ParkItemId));
        }
        catch (IdentifierValidationException exception)
        {
            return ApplicationResult<PassportItemStatisticsResult>.Failure(
                PassportApplicationErrors.InvalidIdentifier(
                    exception.ErrorCode,
                    exception.Message,
                    exception.ParamName));
        }

        Task<IReadOnlyCollection<PassportItemRideObservation>> observationsTask =
            this.sourceReader.ReadAsync(userId, parkItemId, cancellationToken);
        Task<UserRating?> currentRatingTask = this.ratingRepository.GetUserRatingAsync(
            userId,
            RatingTargetType.ParkItem,
            parkItemId,
            cancellationToken);
        await Task.WhenAll(observationsTask, currentRatingTask);

        UserRating? currentRating = await currentRatingTask;
        PassportItemStatistics statistics = PassportItemStatisticsCalculator.Calculate(
            await observationsTask,
            currentRating is null ? null : RatingValue.FromDouble(currentRating.Value));
        PassportItemStatisticsResult result = new PassportItemStatisticsResult(
            parkItemId,
            statistics.RideCount,
            statistics.VisitCount,
            new PassportItemRatingCoverageResult(
                statistics.RatedRideCount,
                statistics.RideCount,
                statistics.RatingCoverageRate),
            ToResult(statistics.FirstExperience),
            ToResult(statistics.LastExperience),
            ToResult(statistics.Ratings),
            statistics.CurrentGlobalRating?.DoubleValue,
            statistics.CurrentGlobalMinusHistoricalAverage);
        return ApplicationResult<PassportItemStatisticsResult>.Success(result);
    }

    private static PassportItemExperienceResult? ToResult(PassportItemExperience? experience)
    {
        return experience is null
            ? null
            : new PassportItemExperienceResult(
                experience.VisitId,
                new VisitDateResult(
                    experience.VisitDate.Year,
                    experience.VisitDate.Month,
                    experience.VisitDate.Day,
                    experience.VisitDate.Precision,
                    experience.VisitDate.IsApproximate));
    }

    private static PassportItemHistoricalRatingsResult? ToResult(
        PassportItemRatingStatistics? ratings)
    {
        return ratings is null
            ? null
            : new PassportItemHistoricalRatingsResult(
                ratings.RatingCount,
                ratings.Average,
                ratings.Median,
                ratings.Minimum,
                ratings.Maximum,
                ratings.PopulationStandardDeviation);
    }
}
