using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Application.Features.Passport.Queries;
using AmusementPark.Application.Features.Passport.Results;
using AmusementPark.Application.Features.Passport.Services;
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
    private readonly IParkItemNameReadRepository parkItemNameReadRepository;

    public GetPassportItemStatisticsQueryHandler(
        IPassportItemStatisticsSourceReader sourceReader,
        IRatingRepository ratingRepository,
        IParkItemNameReadRepository parkItemNameReadRepository)
    {
        this.sourceReader = sourceReader;
        this.ratingRepository = ratingRepository;
        this.parkItemNameReadRepository = parkItemNameReadRepository;
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
        IReadOnlyDictionary<string, string?> parkItemNames =
            statistics.RideCount > 0 || currentRating is not null
                ? await this.parkItemNameReadRepository.GetNamesByIdsAsync(
                    new[] { parkItemId },
                    cancellationToken)
                : new Dictionary<string, string?>(StringComparer.Ordinal);
        PassportItemStatisticsResult result =
            PassportStatisticsResultFactory.CreateItem(
                parkItemId,
                statistics,
                parkItemNames.GetValueOrDefault(parkItemId));
        return ApplicationResult<PassportItemStatisticsResult>.Success(result);
    }
}
