using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Features.Watchlists.Handlers;
using AmusementPark.Application.Features.Watchlists.Ports;
using AmusementPark.Application.Features.Watchlists.Queries;
using AmusementPark.Application.Features.Watchlists.Results;
using AmusementPark.Core.Domain.FactualEvents;
using AmusementPark.Core.Domain.Watchlists;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Contracts.Watchlists;
using AmusementPark.WebAPI.Controllers;
using AmusementPark.WebAPI.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Controllers;

public sealed class AdminNotificationDigestsControllerTests
{
    private static readonly DateTime PeriodStartUtc =
        new DateTime(2026, 9, 14, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task PreviewAsync_ShouldExcludePausedSubscriptionsFromEligibleCount()
    {
        WatchSubscription subscription = CreateSubscription();
        subscription.Pause(PeriodStartUtc.AddHours(2));
        NotificationDigest digest = NotificationDigest.CreateSnapshot(
            "user-1",
            NotificationChannel.Email,
            NotificationFrequency.WeeklyDigest,
            PeriodStartUtc,
            new[] { CreateEntry(subscription.Id) },
            1,
            PeriodStartUtc.AddHours(1));
        Mock<INotificationDigestRepository> digests =
            new Mock<INotificationDigestRepository>(MockBehavior.Strict);
        digests.Setup(repository => repository.GetAsync(digest.Id, CancellationToken.None))
            .ReturnsAsync(digest);
        Mock<IWatchSubscriptionRepository> subscriptions =
            new Mock<IWatchSubscriptionRepository>(MockBehavior.Strict);
        subscriptions.Setup(repository => repository.ListOwnedByIdsAsync(
                "user-1",
                It.IsAny<IReadOnlyCollection<WatchSubscriptionId>>(),
                CancellationToken.None))
            .ReturnsAsync(new[] { subscription });
        AdminNotificationDigestsController controller = new AdminNotificationDigestsController(
            new PreviewNotificationDigestQueryHandler(digests.Object, subscriptions.Object));

        IActionResult response = await controller.PreviewAsync(
            new NotificationDigestPreviewRequestDto
            {
                UserId = "user-1",
                Frequency = "WeeklyDigest",
                PeriodStartUtc = PeriodStartUtc,
            },
            CancellationToken.None);

        NotificationDigestPreviewDto body = Assert.IsType<NotificationDigestPreviewDto>(
            Assert.IsType<OkObjectResult>(response).Value);
        Assert.Equal(1, body.GroupedItemCount);
        Assert.Equal(0, body.EligibleItemCount);
        Assert.Equal(1, body.SuppressedItemCount);
        digests.VerifyAll();
        subscriptions.VerifyAll();
    }

    [Fact]
    public async Task PreviewAsync_WithUnalignedPeriod_ShouldReturnBadRequest()
    {
        AdminNotificationDigestsController controller = new AdminNotificationDigestsController(
            Mock.Of<IQueryHandler<PreviewNotificationDigestQuery, NotificationDigestPreviewResult?>>());

        IActionResult response = await controller.PreviewAsync(
            new NotificationDigestPreviewRequestDto
            {
                UserId = "user-1",
                Frequency = "DailyDigest",
                PeriodStartUtc = PeriodStartUtc.AddHours(1),
            },
            CancellationToken.None);

        Assert.IsType<BadRequestResult>(response);
    }

    [Fact]
    public async Task PreviewAsync_ShouldSuppressEventTypeRemovedFromSubscription()
    {
        WatchSubscription subscription = WatchSubscription.Create(
            WatchSubscriptionId.Parse("subscription-1"),
            "user-1",
            CollectionTargetType.Park,
            "park-1",
            new[] { FactualEventType.OperatorChanged },
            NotificationFrequency.WeeklyDigest,
            new[] { NotificationChannel.Email },
            PeriodStartUtc.AddHours(-1));
        NotificationDigest digest = NotificationDigest.CreateSnapshot(
            "user-1",
            NotificationChannel.Email,
            NotificationFrequency.WeeklyDigest,
            PeriodStartUtc,
            new[] { CreateEntry(subscription.Id) },
            1,
            PeriodStartUtc.AddHours(1));
        Mock<INotificationDigestRepository> digests =
            new Mock<INotificationDigestRepository>(MockBehavior.Strict);
        digests.Setup(repository => repository.GetAsync(digest.Id, CancellationToken.None))
            .ReturnsAsync(digest);
        Mock<IWatchSubscriptionRepository> subscriptions =
            new Mock<IWatchSubscriptionRepository>(MockBehavior.Strict);
        subscriptions.Setup(repository => repository.ListOwnedByIdsAsync(
                "user-1",
                It.IsAny<IReadOnlyCollection<WatchSubscriptionId>>(),
                CancellationToken.None))
            .ReturnsAsync(new[] { subscription });
        AdminNotificationDigestsController controller = new AdminNotificationDigestsController(
            new PreviewNotificationDigestQueryHandler(digests.Object, subscriptions.Object));

        IActionResult response = await controller.PreviewAsync(
            new NotificationDigestPreviewRequestDto
            {
                UserId = "user-1",
                Frequency = "WeeklyDigest",
                PeriodStartUtc = PeriodStartUtc,
            },
            CancellationToken.None);

        NotificationDigestPreviewDto body = Assert.IsType<NotificationDigestPreviewDto>(
            Assert.IsType<OkObjectResult>(response).Value);
        Assert.Equal(0, body.EligibleItemCount);
        Assert.Equal(1, body.SuppressedItemCount);
        digests.VerifyAll();
        subscriptions.VerifyAll();
    }

    [Fact]
    public void Controller_ShouldRequireAdminAndActivatedAccount()
    {
        Type controllerType = typeof(AdminNotificationDigestsController);
        AuthorizeAttribute authorize = Assert.Single(
            controllerType.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
                .Cast<AuthorizeAttribute>(),
            static attribute => !string.IsNullOrWhiteSpace(attribute.Roles));

        Assert.Equal(AuthorizationRoleGroups.Admin, authorize.Roles);
        Assert.Single(controllerType.GetCustomAttributes(
            typeof(RequireActivatedUnblockedUserAttribute),
            inherit: true));
    }

    private static WatchSubscription CreateSubscription()
    {
        return WatchSubscription.Create(
            WatchSubscriptionId.Parse("subscription-1"),
            "user-1",
            CollectionTargetType.Park,
            "park-1",
            new[] { FactualEventType.ParkNameChanged },
            NotificationFrequency.WeeklyDigest,
            new[] { NotificationChannel.Email },
            PeriodStartUtc.AddHours(-1));
    }

    private static NotificationDigestEntry CreateEntry(WatchSubscriptionId subscriptionId)
    {
        return new NotificationDigestEntry(
            FactualChangeEventId.Parse("event-1"),
            subscriptionId,
            "park:park-1:name",
            1,
            FactualEventType.ParkNameChanged,
            FactualTargetType.Park,
            "park-1",
            FactualChangeStatus.Published,
            PeriodStartUtc.AddHours(1));
    }
}
