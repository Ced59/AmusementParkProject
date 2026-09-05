using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Ratings.Queries;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Application.Features.Ratings.Services;
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
            RatingMethodologyResultFactory.Create(RatingMethodologyCatalog.Current)));
    }
}
