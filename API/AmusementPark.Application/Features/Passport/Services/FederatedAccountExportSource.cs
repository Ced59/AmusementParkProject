using System.Text.Json;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.ParkFit.Ports;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Core.Domain.ParkFit;
using AmusementPark.Core.Domain.Users;

namespace AmusementPark.Application.Features.Passport.Services;

public sealed class FederatedAccountExportSource : IFederatedAccountExportSource
{
    private const int RatingPageSize = 200;
    private readonly IUserRepository users;
    private readonly IRatingRepository ratings;
    private readonly IParkFitGroupProfileRepository parkFitProfiles;
    private readonly IAccountTripExportSource trips;
    private readonly IAccountCommunityExportSource community;

    public FederatedAccountExportSource(
        IUserRepository users,
        IRatingRepository ratings,
        IParkFitGroupProfileRepository parkFitProfiles,
        IAccountTripExportSource trips,
        IAccountCommunityExportSource community)
    {
        this.users = users;
        this.ratings = ratings;
        this.parkFitProfiles = parkFitProfiles;
        this.trips = trips;
        this.community = community;
    }

    public async Task<FederatedAccountExportData> LoadAsync(
        string userId,
        PassportExportSourceBudget sourceBudget,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentNullException.ThrowIfNull(sourceBudget);

        User user = await this.users.GetByIdAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("The export owner no longer exists.");
        IReadOnlyCollection<AccountRatingExportData> exportedRatings =
            await this.LoadRatingsAsync(userId, cancellationToken);
        IReadOnlyCollection<ParkFitGroupProfile> profiles =
            await this.parkFitProfiles.ListOwnedAsync(userId, cancellationToken);
        IReadOnlyCollection<AccountTripExportData> trips =
            await this.trips.LoadAsync(userId, cancellationToken);
        AccountCommunityExportData community =
            await this.community.LoadAsync(userId, cancellationToken);

        FederatedAccountExportData data = new FederatedAccountExportData(
            MapIdentity(user),
            exportedRatings,
            profiles.Select(MapParkFitProfile).ToArray(),
            trips,
            community);
        long estimatedBytes = JsonSerializer.SerializeToUtf8Bytes(data).LongLength;
        if (!sourceBudget.TryConsume(estimatedBytes))
        {
            throw new PassportExportSizeLimitException();
        }

        return data;
    }

    private async Task<IReadOnlyCollection<AccountRatingExportData>> LoadRatingsAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        List<AccountRatingExportData> results = new List<AccountRatingExportData>();
        int page = 1;
        while (true)
        {
            PagedResult<UserRatingListItemResult> current =
                await this.ratings.GetUserRatingsAsync(
                    userId,
                    page,
                    RatingPageSize,
                    null,
                    cancellationToken);
            results.AddRange(current.Items.Select(MapRating));
            if (page >= current.TotalPages)
            {
                break;
            }

            page++;
        }

        return results;
    }

    private static AccountIdentityExportData MapIdentity(User user)
    {
        return new AccountIdentityExportData(
            user.FirstName,
            user.LastName,
            user.ResolvePublicDisplayName(),
            user.Email,
            user.IsActivated,
            user.IsBlocked,
            user.PreferredLanguage,
            user.PreferredMeasurementSystem,
            !string.IsNullOrWhiteSpace(user.AvatarUrl),
            user.Roles.Select(static role => role.ToString()).OrderBy(static role => role).ToArray(),
            user.ExternalLogins.Select(static login => new AccountLinkedLoginExportData(
                login.Provider.ToString(),
                login.Email,
                login.IsEmailVerified,
                login.DisplayName,
                login.LinkedAtUtc,
                login.LastLoginAtUtc)).ToArray(),
            user.CreatedAtUtc,
            user.UpdatedAtUtc,
            user.LastLoginUtc,
            user.LastActivityUtc);
    }

    private static AccountRatingExportData MapRating(UserRatingListItemResult rating)
    {
        return new AccountRatingExportData(
            rating.TargetType,
            rating.TargetName,
            rating.ParkName,
            rating.ParkItemCategory,
            rating.ParkItemType,
            rating.Value,
            rating.UpdatedAtUtc);
    }

    private static AccountParkFitProfileExportData MapParkFitProfile(
        ParkFitGroupProfile profile)
    {
        return new AccountParkFitProfileExportData(
            profile.Alias,
            profile.HeightCentimeters,
            profile.AgeYears,
            profile.CanBeAccompanied,
            profile.CompanionAgeYears,
            profile.CreatedAtUtc,
            profile.UpdatedAtUtc);
    }
}
