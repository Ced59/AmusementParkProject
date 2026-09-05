using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Ratings.Queries;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Application.Features.Ratings.Services;
using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Handlers;

public sealed class ListRatingMethodologiesQueryHandler
    : IQueryHandler<ListRatingMethodologiesQuery, ApplicationResult<IReadOnlyCollection<RatingMethodologyResult>>>
{
    public Task<ApplicationResult<IReadOnlyCollection<RatingMethodologyResult>>> HandleAsync(
        ListRatingMethodologiesQuery query,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyCollection<RatingMethodologyResult> results = RatingMethodologyCatalog.All
            .Select(RatingMethodologyResultFactory.Create)
            .ToList()
            .AsReadOnly();
        return Task.FromResult(ApplicationResult<IReadOnlyCollection<RatingMethodologyResult>>.Success(results));
    }
}
