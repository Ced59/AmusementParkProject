using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Ratings.Queries;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Application.Validation;

namespace AmusementPark.Application.Features.Ratings.Handlers;

public sealed class GetParkItemRatingRankingsQueryHandler
    : IQueryHandler<GetParkItemRatingRankingsQuery, ApplicationResult<PagedResult<ParkItemRatingRankingResult>>>
{
    private readonly PagedQueryValidator pagedQueryValidator;
    private readonly ICanonicalParkItemRatingRankingReader canonicalRankingReader;

    public GetParkItemRatingRankingsQueryHandler(
        PagedQueryValidator pagedQueryValidator,
        ICanonicalParkItemRatingRankingReader canonicalRankingReader)
    {
        this.pagedQueryValidator = pagedQueryValidator;
        this.canonicalRankingReader = canonicalRankingReader;
    }

    public async Task<ApplicationResult<PagedResult<ParkItemRatingRankingResult>>> HandleAsync(
        GetParkItemRatingRankingsQuery query,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<ApplicationError> errors = this.pagedQueryValidator.Validate(query.Paging);
        if (errors.Count > 0)
        {
            return ApplicationResult<PagedResult<ParkItemRatingRankingResult>>.Failure(errors);
        }

        if (!Enum.IsDefined(query.ParkItemCategory))
        {
            return ApplicationResult<PagedResult<ParkItemRatingRankingResult>>.Failure(
                RatingApplicationErrors.InvalidParkItemCategory());
        }

        PagedResult<ParkItemRatingRankingResult> canonicalResult =
            await this.canonicalRankingReader.ReadAsync(
            query.ParkItemCategory,
            query.Paging.Page,
            query.Paging.PageSize,
            query.Search,
            query.ParkItemType,
            cancellationToken);
        return ApplicationResult<PagedResult<ParkItemRatingRankingResult>>.Success(canonicalResult);
    }
}
