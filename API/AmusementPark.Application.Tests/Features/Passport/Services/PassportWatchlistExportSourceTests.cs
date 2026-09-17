using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Application.Features.Passport.Services;
using AmusementPark.Core.Domain.FactualEvents;
using AmusementPark.Core.Domain.Watchlists;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Passport.Services;

public sealed class PassportWatchlistExportSourceTests
{
    [Fact]
    public async Task LoadAsync_ResolvesReadableTargetsWithoutExposingStoredIdentifiers()
    {
        UserCollectionEntry entry = UserCollectionEntry.Create(
            UserCollectionEntryId.Parse("entry-internal"),
            "user-1",
            CollectionTargetType.Park,
            "park-internal",
            UserCollectionKind.WantToVisit,
            CollectionTargetStatus.Available,
            "À découvrir avec les enfants",
            1,
            null,
            DateTime.UtcNow);
        DateTime periodStartUtc = new DateTime(2026, 9, 14, 0, 0, 0, DateTimeKind.Utc);
        NotificationDigest digest = NotificationDigest.CreateSnapshot(
            "user-1",
            NotificationChannel.Email,
            NotificationFrequency.WeeklyDigest,
            periodStartUtc,
            new[]
            {
                new NotificationDigestEntry(
                    FactualChangeEventId.Parse("digest-event-not-exported"),
                    WatchSubscriptionId.Parse("subscription-internal"),
                    "park:park-internal:name",
                    1,
                    FactualEventType.ParkNameChanged,
                    FactualTargetType.Park,
                    "park-internal",
                    FactualChangeStatus.Published,
                    periodStartUtc.AddHours(1)),
            },
            1,
            periodStartUtc.AddHours(2));
        PassportWatchlistStoredExportData stored = PassportWatchlistStoredExportData.Empty with
        {
            CollectionEntries = new[] { entry },
            Digests = new[] { digest },
        };
        PassportExportSourceBudget sourceBudget = new PassportExportSourceBudget(1_024);
        Mock<IWatchlistExportStore> store = new Mock<IWatchlistExportStore>(MockBehavior.Strict);
        store.Setup(candidate => candidate.LoadAsync(
                "user-1",
                It.Is<PassportExportSourceBudget>(budget => ReferenceEquals(budget, sourceBudget)),
                CancellationToken.None))
            .ReturnsAsync(stored);
        store.Setup(candidate => candidate.LoadTargetsAsync(
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { "park-internal" })),
                It.Is<IReadOnlyCollection<string>>(ids => ids.Count == 0),
                It.Is<PassportExportSourceBudget>(budget => ReferenceEquals(budget, sourceBudget)),
                CancellationToken.None))
            .ReturnsAsync(new PassportWatchlistTargetCatalog(
                new Dictionary<string, PassportWatchlistTargetSnapshot>(StringComparer.Ordinal)
                {
                    ["park-internal"] = new PassportWatchlistTargetSnapshot(
                        CollectionTargetType.Park,
                        CollectionTargetStatus.Available,
                        "Europa-Park",
                        null),
                },
                new Dictionary<string, PassportWatchlistTargetSnapshot>(StringComparer.Ordinal)));
        store.Setup(candidate => candidate.LoadFactualEventsAsync(
                It.Is<IReadOnlyCollection<FactualChangeEventId>>(ids => ids.Count == 0),
                It.Is<PassportExportSourceBudget>(budget => ReferenceEquals(budget, sourceBudget)),
                CancellationToken.None))
            .ReturnsAsync(Array.Empty<FactualChangeEvent>());
        PassportWatchlistExportSource source = new PassportWatchlistExportSource(store.Object);

        PassportWatchlistExportData result = await source.LoadAsync(
            "user-1",
            sourceBudget,
            CancellationToken.None);

        PassportWatchlistTargetSnapshot target = Assert.Single(result.ParkTargets).Value;
        Assert.Equal("Europa-Park", target.Name);
        Assert.Equal("À découvrir avec les enfants", Assert.Single(result.CollectionEntries).PrivateNote);
        store.VerifyAll();
    }
}
