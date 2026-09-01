using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Ratings.Queries;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Handlers;

public sealed class GetCurrentRatingMethodologyQueryHandler
    : IQueryHandler<GetCurrentRatingMethodologyQuery, ApplicationResult<RatingMethodologyResult>>
{
    public Task<ApplicationResult<RatingMethodologyResult>> HandleAsync(
        GetCurrentRatingMethodologyQuery query,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(ApplicationResult<RatingMethodologyResult>.Success(
            ToResult(RatingMethodologyCatalog.Current)));
    }

    internal static RatingMethodologyResult ToResult(RatingMethodologyDefinition definition)
    {
        RankingEligibilityPolicy policy = definition.EligibilityPolicy;
        return new RatingMethodologyResult(
            definition.Version,
            definition.EffectiveDate,
            definition.Version == RatingMethodologyCatalog.Current.Version,
            definition.PreviousVersion,
            definition.RatingMinimum,
            definition.RatingMaximum,
            definition.RatingStep,
            definition.BayesianPriorMean,
            definition.BayesianPriorWeight,
            definition.ParkDirectScoreWeight,
            definition.ParkItemsScoreWeight,
            definition.BalancesItemCategoriesEqually,
            policy.ProvisionalMinUniqueContributors,
            policy.EligibleMinUniqueContributors,
            policy.EstablishedMinUniqueContributors,
            policy.StrongEvidenceMinUniqueContributors,
            policy.MinimumEligibleEntriesPerRanking,
            policy.MinimumEligibleItemsForParkItemComponent,
            policy.MinimumEligibleItemsPerCategory,
            policy.MinimumEligibleCategories,
            policy.ScoreTieEpsilon,
            definition.RankingConvention);
    }
}

public sealed class GetRatingMethodologyQueryHandler
    : IQueryHandler<GetRatingMethodologyQuery, ApplicationResult<RatingMethodologyResult>>
{
    public Task<ApplicationResult<RatingMethodologyResult>> HandleAsync(
        GetRatingMethodologyQuery query,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        RatingMethodologyDefinition? definition;
        try
        {
            RatingMethodologyVersion version = RatingMethodologyVersion.Parse(query.Version);
            if (!RatingMethodologyCatalog.TryResolve(version, out definition))
            {
                return Task.FromResult(ApplicationResult<RatingMethodologyResult>.Failure(
                    RatingApplicationErrors.MethodologyNotFound()));
            }
        }
        catch (ArgumentException)
        {
            return Task.FromResult(ApplicationResult<RatingMethodologyResult>.Failure(
                RatingApplicationErrors.MethodologyNotFound()));
        }

        return Task.FromResult(ApplicationResult<RatingMethodologyResult>.Success(
            GetCurrentRatingMethodologyQueryHandler.ToResult(definition)));
    }
}

public sealed class ListRatingMethodologiesQueryHandler
    : IQueryHandler<ListRatingMethodologiesQuery, ApplicationResult<IReadOnlyCollection<RatingMethodologyResult>>>
{
    public Task<ApplicationResult<IReadOnlyCollection<RatingMethodologyResult>>> HandleAsync(
        ListRatingMethodologiesQuery query,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyCollection<RatingMethodologyResult> results = RatingMethodologyCatalog.All
            .Select(GetCurrentRatingMethodologyQueryHandler.ToResult)
            .ToList()
            .AsReadOnly();
        return Task.FromResult(ApplicationResult<IReadOnlyCollection<RatingMethodologyResult>>.Success(results));
    }
}
