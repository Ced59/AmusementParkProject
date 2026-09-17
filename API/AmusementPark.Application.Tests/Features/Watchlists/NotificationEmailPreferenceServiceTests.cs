using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Application.Features.Watchlists.Models;
using AmusementPark.Application.Features.Watchlists.Ports;
using AmusementPark.Application.Features.Watchlists.Results;
using AmusementPark.Application.Features.Watchlists.Services;
using AmusementPark.Core.Domain.Users;
using AmusementPark.Core.Domain.Watchlists;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Watchlists;

public sealed class NotificationEmailPreferenceServiceTests
{
    private static readonly DateTime NowUtc =
        new DateTime(2026, 9, 17, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task UpdateAsync_ShouldRequireAnActivatedVerifiedAddress()
    {
        User user = CreateUser(isActivated: false);
        Mock<IUserRepository> users = CreateUserRepository(user);
        Mock<INotificationEmailPreferenceRepository> preferences =
            new Mock<INotificationEmailPreferenceRepository>(MockBehavior.Strict);
        NotificationEmailPreferenceService service = CreateService(preferences, users);

        ApplicationResult<NotificationEmailPreferenceResult> result = await service.UpdateAsync(
            "user-1",
            new NotificationEmailPreferenceInput(true, true, "fr", null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("notification-email-preference.email-unavailable", Assert.Single(result.Errors).Code);
        preferences.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateAsync_ShouldRequireExplicitConsent()
    {
        User user = CreateUser();
        Mock<IUserRepository> users = CreateUserRepository(user);
        Mock<INotificationEmailPreferenceRepository> preferences =
            new Mock<INotificationEmailPreferenceRepository>(MockBehavior.Strict);
        NotificationEmailPreferenceService service = CreateService(preferences, users);

        ApplicationResult<NotificationEmailPreferenceResult> result = await service.UpdateAsync(
            "user-1",
            new NotificationEmailPreferenceInput(true, false, "fr", null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("notification-email-preference.consent-required", Assert.Single(result.Errors).Code);
        preferences.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateAsync_ShouldPersistConsentAndMaskTheAddress()
    {
        User user = CreateUser();
        Mock<IUserRepository> users = CreateUserRepository(user);
        Mock<INotificationEmailPreferenceRepository> preferences =
            new Mock<INotificationEmailPreferenceRepository>(MockBehavior.Strict);
        preferences.Setup(repository => repository.GetAsync("user-1", CancellationToken.None))
            .ReturnsAsync((NotificationEmailPreference?)null);
        preferences.Setup(repository => repository.CreateAsync(
                It.Is<NotificationEmailPreference>(preference =>
                    preference.IsEnabled
                    && preference.ConsentLocale == "fr"
                    && preference.ConsentTextVersion == NotificationEmailPreference.CurrentConsentTextVersion),
                CancellationToken.None))
            .ReturnsAsync(NotificationEmailPreferenceWriteOutcome.Success);
        NotificationEmailPreferenceService service = CreateService(preferences, users);

        ApplicationResult<NotificationEmailPreferenceResult> result = await service.UpdateAsync(
            "user-1",
            new NotificationEmailPreferenceInput(true, true, "fr", null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.True(result.Value.EmailDigestEnabled);
        Assert.Equal("u***@example.com", result.Value.MaskedEmail);
        Assert.Equal(NowUtc, result.Value.ConsentGrantedAtUtc);
        preferences.VerifyAll();
    }

    [Fact]
    public async Task RevokeByTokenAsync_ShouldBeIdempotentWhenNoPreferenceExists()
    {
        Mock<IUserRepository> users = new Mock<IUserRepository>(MockBehavior.Strict);
        Mock<INotificationEmailPreferenceRepository> preferences =
            new Mock<INotificationEmailPreferenceRepository>(MockBehavior.Strict);
        preferences.Setup(repository => repository.GetAsync("user-1", CancellationToken.None))
            .ReturnsAsync((NotificationEmailPreference?)null);
        NotificationEmailPreferenceService service = CreateService(preferences, users);

        ApplicationResult result = await service.RevokeByTokenAsync("user-1", CancellationToken.None);

        Assert.True(result.IsSuccess);
        preferences.VerifyAll();
        users.VerifyNoOtherCalls();
    }

    private static NotificationEmailPreferenceService CreateService(
        Mock<INotificationEmailPreferenceRepository> preferences,
        Mock<IUserRepository> users)
    {
        Mock<TimeProvider> timeProvider = new Mock<TimeProvider>(MockBehavior.Strict);
        timeProvider.Setup(provider => provider.GetUtcNow()).Returns(new DateTimeOffset(NowUtc));
        return new NotificationEmailPreferenceService(
            preferences.Object,
            users.Object,
            timeProvider.Object);
    }

    private static Mock<IUserRepository> CreateUserRepository(User user)
    {
        Mock<IUserRepository> users = new Mock<IUserRepository>(MockBehavior.Strict);
        users.Setup(repository => repository.GetByIdAsync("user-1", CancellationToken.None))
            .ReturnsAsync(user);
        return users;
    }

    private static User CreateUser(bool isActivated = true)
    {
        return new User
        {
            Id = "user-1",
            Email = "user@example.com",
            IsActivated = isActivated,
            IsBlocked = false,
        };
    }
}
