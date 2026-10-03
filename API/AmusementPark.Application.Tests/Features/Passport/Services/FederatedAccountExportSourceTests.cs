using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.ParkFit.Ports;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Application.Features.Passport.Services;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Core.Domain.ParkFit;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Core.Domain.Users;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Passport.Services;

public sealed class FederatedAccountExportSourceTests
{
    [Fact]
    public async Task LoadAsync_FederatesReadableAccountDataWithoutSecretsOrTechnicalIds()
    {
        DateTime nowUtc = new DateTime(2026, 10, 3, 18, 0, 0, DateTimeKind.Utc);
        User user = new User
        {
            Id = "internal-user-id",
            FirstName = "Camille",
            LastName = "Martin",
            PublicDisplayName = "CoasterCamille",
            Email = "camille@example.com",
            AvatarUrl = "/images/internal-avatar-id-must-not-leak",
            HashedPassword = "password-hash-must-not-leak",
            IsActivated = true,
            PreferredLanguage = "fr",
            PreferredMeasurementSystem = "Metric",
            Roles = new List<Role> { Role.User },
            ExternalLogins = new List<ExternalLogin>
            {
                new ExternalLogin
                {
                    Provider = ExternalLoginProvider.Google,
                    ProviderUserId = "provider-user-id-must-not-leak",
                    Email = "camille@example.com",
                    IsEmailVerified = true,
                    DisplayName = "Camille",
                    LinkedAtUtc = nowUtc,
                    LastLoginAtUtc = nowUtc,
                },
            },
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc,
            LastLoginUtc = nowUtc,
            LastActivityUtc = nowUtc,
        };
        Mock<IUserRepository> users = new Mock<IUserRepository>(MockBehavior.Strict);
        users.Setup(repository => repository.GetByIdAsync(
                "internal-user-id",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        UserRatingListItemResult rating = new UserRatingListItemResult(
            "internal-rating-id",
            RatingTargetType.ParkItem,
            "internal-target-id",
            "Silver Star",
            "internal-park-id",
            "Europa Park",
            ParkItemCategory.Attraction,
            ParkItemType.RollerCoaster,
            4.5,
            nowUtc,
            new RatingSummaryResult(
                RatingTargetType.ParkItem,
                "internal-target-id",
                42,
                4.4,
                4.3));
        Mock<IRatingRepository> ratings = new Mock<IRatingRepository>(MockBehavior.Strict);
        ratings.Setup(repository => repository.GetUserRatingsAsync(
                "internal-user-id",
                1,
                200,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<UserRatingListItemResult>(
                new[] { rating },
                1,
                200,
                1));
        ParkFitGroupProfile profile = ParkFitGroupProfile.Create(
            ParkFitGroupProfileId.Parse("internal-profile-id"),
            "internal-user-id",
            "Enfant",
            120,
            8,
            true,
            40,
            nowUtc);
        Mock<IParkFitGroupProfileRepository> profiles =
            new Mock<IParkFitGroupProfileRepository>(MockBehavior.Strict);
        profiles.Setup(repository => repository.ListOwnedAsync(
                "internal-user-id",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { profile });
        Mock<IAccountTripExportSource> trips =
            new Mock<IAccountTripExportSource>(MockBehavior.Strict);
        trips.Setup(source => source.LoadAsync(
                "internal-user-id",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<AccountTripExportData>());
        Mock<IAccountCommunityExportSource> community =
            new Mock<IAccountCommunityExportSource>(MockBehavior.Strict);
        community.Setup(source => source.LoadAsync(
                "internal-user-id",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(AccountCommunityExportData.Empty);
        FederatedAccountExportSource source = new FederatedAccountExportSource(
            users.Object,
            ratings.Object,
            profiles.Object,
            trips.Object,
            community.Object);
        PassportExportSourceBudget budget = new PassportExportSourceBudget(1024 * 1024);

        FederatedAccountExportData result = await source.LoadAsync(
            "internal-user-id",
            budget,
            CancellationToken.None);

        Assert.Equal("CoasterCamille", result.Identity.PublicDisplayName);
        Assert.True(result.Identity.HasAvatar);
        Assert.Equal("Google", Assert.Single(result.Identity.LinkedLogins).Provider);
        Assert.Equal("Silver Star", Assert.Single(result.Ratings).TargetName);
        Assert.Equal("Enfant", Assert.Single(result.ParkFitProfiles).Alias);
        Assert.True(budget.ConsumedBytes > 0);
        string serialized = System.Text.Json.JsonSerializer.Serialize(result);
        Assert.DoesNotContain("internal-user-id", serialized, StringComparison.Ordinal);
        Assert.DoesNotContain("internal-rating-id", serialized, StringComparison.Ordinal);
        Assert.DoesNotContain("internal-target-id", serialized, StringComparison.Ordinal);
        Assert.DoesNotContain("internal-park-id", serialized, StringComparison.Ordinal);
        Assert.DoesNotContain("internal-profile-id", serialized, StringComparison.Ordinal);
        Assert.DoesNotContain("password-hash-must-not-leak", serialized, StringComparison.Ordinal);
        Assert.DoesNotContain("provider-user-id-must-not-leak", serialized, StringComparison.Ordinal);
        Assert.DoesNotContain("internal-avatar-id-must-not-leak", serialized, StringComparison.Ordinal);
        users.VerifyAll();
        ratings.VerifyAll();
        profiles.VerifyAll();
        trips.VerifyAll();
        community.VerifyAll();
    }

    [Fact]
    public async Task LoadAsync_WhenFederatedDataExceedsBudget_RejectsWithoutTruncating()
    {
        User user = new User
        {
            Id = "user-1",
            Email = new string('a', 200),
        };
        Mock<IUserRepository> users = new Mock<IUserRepository>(MockBehavior.Strict);
        users.Setup(repository => repository.GetByIdAsync(
                "user-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        Mock<IRatingRepository> ratings = new Mock<IRatingRepository>(MockBehavior.Strict);
        ratings.Setup(repository => repository.GetUserRatingsAsync(
                "user-1",
                1,
                200,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<UserRatingListItemResult>(
                Array.Empty<UserRatingListItemResult>(),
                1,
                200,
                0));
        Mock<IParkFitGroupProfileRepository> profiles =
            new Mock<IParkFitGroupProfileRepository>(MockBehavior.Strict);
        profiles.Setup(repository => repository.ListOwnedAsync(
                "user-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ParkFitGroupProfile>());
        Mock<IAccountTripExportSource> trips =
            new Mock<IAccountTripExportSource>(MockBehavior.Strict);
        trips.Setup(source => source.LoadAsync(
                "user-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<AccountTripExportData>());
        Mock<IAccountCommunityExportSource> community =
            new Mock<IAccountCommunityExportSource>(MockBehavior.Strict);
        community.Setup(source => source.LoadAsync(
                "user-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(AccountCommunityExportData.Empty);
        FederatedAccountExportSource source = new FederatedAccountExportSource(
            users.Object,
            ratings.Object,
            profiles.Object,
            trips.Object,
            community.Object);

        await Assert.ThrowsAsync<PassportExportSizeLimitException>(() => source.LoadAsync(
            "user-1",
            new PassportExportSourceBudget(1),
            CancellationToken.None));
    }
}
