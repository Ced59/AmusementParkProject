using AmusementPark.Application.Features.Watchlists.Ports;
using AmusementPark.Core.Domain.FactualEvents;
using AmusementPark.Core.Domain.Watchlists;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Watchlists;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Driver;
using Moq;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class UserNotificationRepositoryTests
{
    [Fact]
    public async Task CreateManyAsync_ShouldHoldActivityLeaseUntilACancelledWriteHasExited()
    {
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        CancellationToken workerToken = cancellation.Token;
        Mock<IWatchlistAccountDeletionFence> fence =
            new Mock<IWatchlistAccountDeletionFence>(MockBehavior.Strict);
        fence.SetupSequence(candidate => candidate.ListBlockedAsync(
                It.IsAny<IReadOnlyCollection<string>>(),
                workerToken))
            .ReturnsAsync(new HashSet<string>(StringComparer.Ordinal))
            .ThrowsAsync(new OperationCanceledException(workerToken));
        fence.Setup(candidate => candidate.TryAcquireActivityLeaseAsync(
                "user-1",
                TimeSpan.FromMinutes(3),
                workerToken))
            .ReturnsAsync("activity-lease-1");
        fence.Setup(candidate => candidate.ReleaseActivityLeaseAsync(
                "activity-lease-1",
                CancellationToken.None))
            .Returns(Task.CompletedTask);
        BulkWriteResult<UserNotificationDocument> writeResult =
            new BulkWriteResult<UserNotificationDocument>.Acknowledged(
                1,
                0,
                0,
                0,
                0,
                Array.Empty<WriteModel<UserNotificationDocument>>(),
                Array.Empty<BulkWriteUpsert>());
        Mock<IMongoCollection<UserNotificationDocument>> collection =
            new Mock<IMongoCollection<UserNotificationDocument>>(MockBehavior.Strict);
        collection.Setup(candidate => candidate.BulkWriteAsync(
                It.IsAny<IEnumerable<WriteModel<UserNotificationDocument>>>(),
                It.Is<BulkWriteOptions>(options => !options.IsOrdered),
                workerToken))
            .Callback(cancellation.Cancel)
            .ReturnsAsync(writeResult);
        UserNotification notification = CreateNotification();
        UserNotificationRepository repository = new UserNotificationRepository(
            collection.Object,
            fence.Object);

        await Assert.ThrowsAsync<OperationCanceledException>(() => repository.CreateManyAsync(
            new[] { notification },
            workerToken));

        fence.VerifyAll();
        collection.VerifyAll();
    }

    [Fact]
    public void IsDuplicateOnlyFailure_WithNoWriteError_ShouldPropagateFailure()
    {
        bool result = UserNotificationRepository.IsDuplicateOnlyFailure(
            Array.Empty<ServerErrorCategory>(),
            hasWriteConcernError: false);

        Assert.False(result);
    }

    [Fact]
    public void IsDuplicateOnlyFailure_WithWriteConcernError_ShouldPropagateFailure()
    {
        bool result = UserNotificationRepository.IsDuplicateOnlyFailure(
            new[] { ServerErrorCategory.DuplicateKey },
            hasWriteConcernError: true);

        Assert.False(result);
    }

    [Fact]
    public void IsDuplicateOnlyFailure_WithMixedWriteErrors_ShouldPropagateFailure()
    {
        bool result = UserNotificationRepository.IsDuplicateOnlyFailure(
            new[] { ServerErrorCategory.DuplicateKey, ServerErrorCategory.Uncategorized },
            hasWriteConcernError: false);

        Assert.False(result);
    }

    [Fact]
    public void IsDuplicateOnlyFailure_WithDuplicateWriteErrors_ShouldAllowIdempotency()
    {
        bool result = UserNotificationRepository.IsDuplicateOnlyFailure(
            new[] { ServerErrorCategory.DuplicateKey },
            hasWriteConcernError: false);

        Assert.True(result);
    }

    private static UserNotification CreateNotification()
    {
        DateTime deliveredAtUtc = new DateTime(2026, 9, 17, 10, 0, 0, DateTimeKind.Utc);
        return UserNotification.Restore(
            UserNotificationId.Parse("notification-1"),
            "user-1",
            FactualChangeEventId.Parse("event-1"),
            WatchSubscriptionId.Parse("subscription-1"),
            FactualEventType.ParkNameChanged,
            FactualTargetType.Park,
            "park-1",
            "park-1",
            1,
            UserNotification.CurrentTemplateVersion,
            "FR",
            UserNotificationStatus.Delivered,
            deliveredAtUtc,
            deliveredAtUtc,
            null,
            null,
            deliveredAtUtc.AddDays(UserNotification.RetentionDays),
            1);
    }
}
