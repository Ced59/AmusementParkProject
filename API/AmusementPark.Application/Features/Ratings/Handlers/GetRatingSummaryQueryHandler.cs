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

public sealed class GetRatingSummaryQueryHandler : IQueryHandler<GetRatingSummaryQuery, ApplicationResult<RatingSummaryResult>>
{
    private readonly IRatingRepository ratingRepository;
    private readonly IRatingRankProvider ratingRankProvider;
    private readonly IParkRepository parkRepository;
    private readonly IParkItemRepository parkItemRepository;

    public GetRatingSummaryQueryHandler(
        IRatingRepository ratingRepository,
        IRatingRankProvider ratingRankProvider,
        IParkRepository parkRepository,
        IParkItemRepository parkItemRepository)
    {
        this.ratingRepository = ratingRepository;
        this.ratingRankProvider = ratingRankProvider;
        this.parkRepository = parkRepository;
        this.parkItemRepository = parkItemRepository;
    }

    public async Task<ApplicationResult<RatingSummaryResult>> HandleAsync(GetRatingSummaryQuery query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.TargetId))
        {
            return ApplicationResult<RatingSummaryResult>.Failure(ApplicationErrors.Required(nameof(query.TargetId)));
        }

        if (!Enum.IsDefined(query.TargetType))
        {
            return ApplicationResult<RatingSummaryResult>.Failure(RatingApplicationErrors.InvalidTargetType());
        }

        string targetId = query.TargetId.Trim();
        RatingTargetMetadataResult? metadata = await RatingTargetMetadataResolver.ResolveAsync(
            query.TargetType,
            targetId,
            this.parkRepository,
            this.parkItemRepository,
            cancellationToken);
        if (metadata is null)
        {
            return ApplicationResult<RatingSummaryResult>.Failure(RatingApplicationErrors.TargetNotFound());
        }

        if (!metadata.CanReceiveVisitorRatings)
        {
            return ApplicationResult<RatingSummaryResult>.Failure(RatingApplicationErrors.TargetUnavailable());
        }

        RatingAggregate? aggregate = await this.ratingRepository.GetAggregateAsync(query.TargetType, targetId, cancellationToken);
        RatingSummaryResult summary = RatingResultFactory.CreateSummary(
            query.TargetType,
            targetId,
            aggregate,
            metadata.CanReceiveVisitorRatings,
            aggregateIntegrityIsValid: aggregate is null ? true : null);
        if (aggregate is not null && aggregate.RatingCount > 0)
        {
            RatingPublishedRank? publishedRank = await this.ratingRankProvider.GetRankAsync(
                aggregate,
                cancellationToken);
            summary = summary with
            {
                Rank = publishedRank?.Rank,
                GeneratedAtUtc = publishedRank?.GeneratedAtUtc,
                MethodologyVersion = publishedRank?.MethodologyVersion
                    ?? summary.MethodologyVersion,
            };
        }

        return ApplicationResult<RatingSummaryResult>.Success(summary);
    }
}
