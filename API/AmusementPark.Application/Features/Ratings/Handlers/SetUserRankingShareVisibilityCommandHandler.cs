using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Ratings.Commands;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Handlers;

public sealed class SetUserRankingShareVisibilityCommandHandler
    : ICommandHandler<SetUserRankingShareVisibilityCommand, ApplicationResult<UserRankingShareSettingsResult>>
{
    private readonly IUserRankingShareRepository shareRepository;
    private readonly IUserRankingShareIdFactory shareIdFactory;
    private readonly TimeProvider timeProvider;

    public SetUserRankingShareVisibilityCommandHandler(
        IUserRankingShareRepository shareRepository,
        IUserRankingShareIdFactory shareIdFactory,
        TimeProvider? timeProvider = null)
    {
        this.shareRepository = shareRepository;
        this.shareIdFactory = shareIdFactory;
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<ApplicationResult<UserRankingShareSettingsResult>> HandleAsync(
        SetUserRankingShareVisibilityCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.UserId))
        {
            return ApplicationResult<UserRankingShareSettingsResult>.Failure(
                ApplicationErrors.Required(nameof(command.UserId)));
        }

        string userId = command.UserId.Trim();
        DateTime nowUtc = this.timeProvider.GetUtcNow().UtcDateTime;
        UserRankingShare? share = await this.shareRepository.GetByUserIdAsync(userId, cancellationToken);
        if (share is null)
        {
            if (!command.IsPublic)
            {
                return ApplicationResult<UserRankingShareSettingsResult>.Success(
                    GetUserRankingShareSettingsQueryHandler.ToSettings(null));
            }

            share = UserRankingShare.Create(userId, nowUtc);
        }

        if (command.IsPublic)
        {
            share.Publish(this.shareIdFactory.Generate(), nowUtc);
        }
        else
        {
            share.Revoke(nowUtc);
        }

        UserRankingShare savedShare = await this.shareRepository.UpsertAsync(share, cancellationToken);
        return ApplicationResult<UserRankingShareSettingsResult>.Success(
            GetUserRankingShareSettingsQueryHandler.ToSettings(savedShare));
    }
}
