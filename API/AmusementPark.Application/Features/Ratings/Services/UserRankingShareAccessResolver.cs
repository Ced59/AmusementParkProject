using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Core.Domain.Users;

namespace AmusementPark.Application.Features.Ratings.Services;

public sealed record UserRankingShareOwner(
    string UserId,
    string DisplayName,
    DateTime PublishedAtUtc);

public sealed class UserRankingShareAccessResolver
{
    private readonly IUserRankingShareRepository shareRepository;
    private readonly IUserRepository userRepository;

    public UserRankingShareAccessResolver(
        IUserRankingShareRepository shareRepository,
        IUserRepository userRepository)
    {
        this.shareRepository = shareRepository;
        this.userRepository = userRepository;
    }

    public async Task<ApplicationResult<UserRankingShareOwner>> ResolveAsync(
        string shareId,
        CancellationToken cancellationToken)
    {
        string normalizedShareId = shareId?.Trim() ?? string.Empty;
        if (!IsValidShareId(normalizedShareId))
        {
            return ApplicationResult<UserRankingShareOwner>.Failure(
                RatingApplicationErrors.SharedRankingNotFound());
        }

        UserRankingShare? share = await this.shareRepository.GetPublicByShareIdAsync(
            normalizedShareId,
            cancellationToken);
        if (share is null || !share.IsPublic || share.PublishedAtUtc is null)
        {
            return ApplicationResult<UserRankingShareOwner>.Failure(
                RatingApplicationErrors.SharedRankingNotFound());
        }

        User? user = await this.userRepository.GetByIdAsync(share.UserId, cancellationToken);
        if (user is null || !user.IsActivated || user.IsBlocked)
        {
            return ApplicationResult<UserRankingShareOwner>.Failure(
                RatingApplicationErrors.SharedRankingNotFound());
        }

        string displayName = user.ResolvePublicDisplayName()?.Trim() ?? string.Empty;
        if (displayName.Length == 0)
        {
            displayName = "User";
        }

        return ApplicationResult<UserRankingShareOwner>.Success(new UserRankingShareOwner(
            user.Id,
            displayName,
            share.PublishedAtUtc.Value));
    }

    private static bool IsValidShareId(string shareId)
    {
        return shareId.Length is >= 32 and <= 128
            && shareId.All(static character =>
                char.IsAsciiLetterOrDigit(character) || character is '-' or '_');
    }
}
