using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Ratings.Queries;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Handlers;

public sealed class GetUserRankingShareSettingsQueryHandler
    : IQueryHandler<GetUserRankingShareSettingsQuery, ApplicationResult<UserRankingShareSettingsResult>>
{
    private readonly IUserRankingShareRepository shareRepository;

    public GetUserRankingShareSettingsQueryHandler(IUserRankingShareRepository shareRepository)
    {
        this.shareRepository = shareRepository;
    }

    public async Task<ApplicationResult<UserRankingShareSettingsResult>> HandleAsync(
        GetUserRankingShareSettingsQuery query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.UserId))
        {
            return ApplicationResult<UserRankingShareSettingsResult>.Failure(
                ApplicationErrors.Required(nameof(query.UserId)));
        }

        UserRankingShare? share = await this.shareRepository.GetByUserIdAsync(
            query.UserId.Trim(),
            cancellationToken);
        return ApplicationResult<UserRankingShareSettingsResult>.Success(ToSettings(share));
    }

    internal static UserRankingShareSettingsResult ToSettings(UserRankingShare? share)
    {
        return new UserRankingShareSettingsResult(
            share?.IsPublic == true,
            share?.IsPublic == true ? share.ShareId : null,
            share?.IsPublic == true ? share.PublishedAtUtc : null);
    }
}
