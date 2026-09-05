using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Ratings.Models;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Ratings.Queries;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Application.Features.Ratings.Services;
using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Handlers;

public sealed class GetUserRatingQueryHandler : IQueryHandler<GetUserRatingQuery, ApplicationResult<UserRatingResult?>>
{
    private readonly IRatingRepository ratingRepository;
    private readonly IParkRepository parkRepository;
    private readonly IParkItemRepository parkItemRepository;

    public GetUserRatingQueryHandler(
        IRatingRepository ratingRepository,
        IParkRepository parkRepository,
        IParkItemRepository parkItemRepository)
    {
        this.ratingRepository = ratingRepository;
        this.parkRepository = parkRepository;
        this.parkItemRepository = parkItemRepository;
    }

    public async Task<ApplicationResult<UserRatingResult?>> HandleAsync(GetUserRatingQuery query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.UserId))
        {
            return ApplicationResult<UserRatingResult?>.Failure(ApplicationErrors.Required(nameof(query.UserId)));
        }

        if (string.IsNullOrWhiteSpace(query.TargetId))
        {
            return ApplicationResult<UserRatingResult?>.Failure(ApplicationErrors.Required(nameof(query.TargetId)));
        }

        if (!Enum.IsDefined(query.TargetType))
        {
            return ApplicationResult<UserRatingResult?>.Failure(RatingApplicationErrors.InvalidTargetType());
        }

        UserRating? rating = await this.ratingRepository.GetUserRatingAsync(query.UserId.Trim(), query.TargetType, query.TargetId.Trim(), cancellationToken);
        if (rating is null)
        {
            return ApplicationResult<UserRatingResult?>.Success(null);
        }

        string targetId = query.TargetId.Trim();
        RatingTargetMetadataResult? metadata = await RatingTargetMetadataResolver.ResolveAsync(
            query.TargetType,
            targetId,
            this.parkRepository,
            this.parkItemRepository,
            cancellationToken);
        RatingAggregate? aggregate = await this.ratingRepository.GetAggregateAsync(
            query.TargetType,
            targetId,
            cancellationToken);
        RatingSummaryResult summary = RatingResultFactory.CreateSummary(
            query.TargetType,
            targetId,
            aggregate,
            metadata?.CanReceiveVisitorRatings ?? false,
            aggregateIntegrityIsValid: aggregate is null ? false : null);

        UserRatingResult result = new UserRatingResult(
            rating.Id,
            rating.UserId,
            rating.TargetType,
            rating.TargetId,
            rating.ParkId,
            rating.ParkItemCategory,
            rating.ParkItemType,
            rating.Value,
            rating.CreatedAtUtc,
            rating.UpdatedAtUtc,
            summary);

        return ApplicationResult<UserRatingResult?>.Success(result);
    }
}
