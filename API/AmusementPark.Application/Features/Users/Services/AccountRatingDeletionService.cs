using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Ratings.Commands;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Ratings.Results;

namespace AmusementPark.Application.Features.Users.Services;

public sealed class AccountRatingDeletionService
{
    private const int PageSize = 100;
    private const int MaximumDeletedRatings = 10000;
    private readonly IRatingRepository ratingRepository;
    private readonly ICommandHandler<
        DeleteUserRatingCommand,
        ApplicationResult<RatingSummaryResult>> deleteRatingHandler;

    public AccountRatingDeletionService(
        IRatingRepository ratingRepository,
        ICommandHandler<
            DeleteUserRatingCommand,
            ApplicationResult<RatingSummaryResult>> deleteRatingHandler)
    {
        this.ratingRepository = ratingRepository;
        this.deleteRatingHandler = deleteRatingHandler;
    }

    public async Task<int> DeleteAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        int deletedCount = 0;
        while (true)
        {
            PagedResult<UserRatingListItemResult> page =
                await this.ratingRepository.GetUserRatingsAsync(
                    userId,
                    1,
                    PageSize,
                    null,
                    cancellationToken);
            if (page.Items.Count == 0)
            {
                return deletedCount;
            }

            foreach (UserRatingListItemResult rating in page.Items)
            {
                ApplicationResult<RatingSummaryResult> result =
                    await this.deleteRatingHandler.HandleAsync(
                        new DeleteUserRatingCommand(
                            userId,
                            rating.TargetType,
                            rating.TargetId),
                        cancellationToken);
                if (!result.IsSuccess)
                {
                    throw new InvalidOperationException(
                        $"Unable to delete account rating '{rating.Id}'.");
                }

                deletedCount++;
                if (deletedCount > MaximumDeletedRatings)
                {
                    throw new InvalidOperationException(
                        "The account rating deletion safety limit was exceeded.");
                }
            }
        }
    }
}
