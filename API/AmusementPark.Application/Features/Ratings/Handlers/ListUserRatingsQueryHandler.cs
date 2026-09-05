using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Ratings.Queries;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Application.Validation;

namespace AmusementPark.Application.Features.Ratings.Handlers;

public sealed class ListUserRatingsQueryHandler : IQueryHandler<ListUserRatingsQuery, ApplicationResult<PagedResult<UserRatingListItemResult>>>
{
    private readonly IRatingRepository ratingRepository;
    private readonly PagedQueryValidator pagedQueryValidator;

    public ListUserRatingsQueryHandler(IRatingRepository ratingRepository, PagedQueryValidator pagedQueryValidator)
    {
        this.ratingRepository = ratingRepository;
        this.pagedQueryValidator = pagedQueryValidator;
    }

    public async Task<ApplicationResult<PagedResult<UserRatingListItemResult>>> HandleAsync(ListUserRatingsQuery query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.UserId))
        {
            return ApplicationResult<PagedResult<UserRatingListItemResult>>.Failure(ApplicationErrors.Required(nameof(query.UserId)));
        }

        IReadOnlyCollection<ApplicationError> errors = this.pagedQueryValidator.Validate(query.Paging);
        if (errors.Count > 0)
        {
            return ApplicationResult<PagedResult<UserRatingListItemResult>>.Failure(errors);
        }

        PagedResult<UserRatingListItemResult> result = await this.ratingRepository.GetUserRatingsAsync(
            query.UserId.Trim(),
            query.Paging.Page,
            query.Paging.PageSize,
            query.ParkSearch,
            cancellationToken);

        return ApplicationResult<PagedResult<UserRatingListItemResult>>.Success(result);
    }
}
