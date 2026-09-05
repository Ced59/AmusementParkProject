using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Ratings.Queries;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Application.Features.Ratings.Services;
using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Handlers;

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
            RatingMethodologyResultFactory.Create(definition)));
    }
}
