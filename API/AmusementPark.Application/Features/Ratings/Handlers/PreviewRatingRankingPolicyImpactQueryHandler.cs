using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Ratings.Queries;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Application.Features.Ratings.Services;
using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Handlers;

public sealed class PreviewRatingRankingPolicyImpactQueryHandler
    : IQueryHandler<PreviewRatingRankingPolicyImpactQuery, ApplicationResult<RatingRankingPolicyImpactResult>>
{
    private readonly RatingRankingPolicyImpactPreviewer previewer;

    public PreviewRatingRankingPolicyImpactQueryHandler(
        RatingRankingPolicyImpactPreviewer previewer)
    {
        this.previewer = previewer;
    }

    public async Task<ApplicationResult<RatingRankingPolicyImpactResult>> HandleAsync(
        PreviewRatingRankingPolicyImpactQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query.Candidate);
        try
        {
            RankingEligibilityPolicy candidatePolicy = query.Candidate.ToDomain();
            if (RatingMethodologyCatalog.TryResolve(candidatePolicy.Version, out _))
            {
                return ApplicationResult<RatingRankingPolicyImpactResult>.Failure(
                    RatingApplicationErrors.RankingPolicyVersionAlreadyPublished());
            }

            RatingRankingPolicyImpactResult result = await this.previewer.PreviewImpactAsync(
                query.Candidate,
                cancellationToken);
            return ApplicationResult<RatingRankingPolicyImpactResult>.Success(result);
        }
        catch (Exception exception) when (exception is ArgumentException or OverflowException)
        {
            return ApplicationResult<RatingRankingPolicyImpactResult>.Failure(
                RatingApplicationErrors.InvalidRankingPolicyCandidate());
        }
    }
}
