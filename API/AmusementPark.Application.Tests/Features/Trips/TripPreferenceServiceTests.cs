using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Trips.Models;
using AmusementPark.Application.Features.Trips.Ports;
using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.Application.Features.Trips.Services;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Trips;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Trips;

public sealed class TripPreferenceServiceTests
{
    [Fact]
    public async Task GetAsync_ShouldExposeOnlyTheCallersOwnChoiceWithPublicCatalogData()
    {
        DateTime nowUtc = new(2027, 3, 4, 10, 0, 0, DateTimeKind.Utc);
        TripPlan trip = CreateTrip(nowUtc);
        TripMember owner = Assert.Single(trip.Members);
        TripParkCandidate candidate = CreateCandidate(trip, owner, nowUtc);
        Park park = new()
        {
            Id = "park-1",
            Name = "Parc test",
            IsVisible = true,
            Status = ParkStatus.Operating,
        };
        ParkItem parkItem = new()
        {
            Id = "item-1",
            ParkId = park.Id,
            Name = "Grand huit",
            Category = ParkItemCategory.Attraction,
            IsVisible = true,
        };
        TripItemPreference preference = TripItemPreference.Create(
            TripItemPreferenceId.New(),
            trip.Id,
            owner.Id,
            trip.OwnerUserId,
            parkItem.Id,
            TripItemPreferenceLevel.MustDo,
            TripItemPreferenceReason.Sensations,
            nowUtc);
        Mock<ITripPlanRepository> trips = new(MockBehavior.Strict);
        trips.Setup(repository => repository.GetAccessibleAsync(
                trip.OwnerUserId,
                trip.Id,
                CancellationToken.None))
            .ReturnsAsync(trip);
        Mock<ITripParkCandidateRepository> candidates = new(MockBehavior.Strict);
        candidates.Setup(repository => repository.ListAsync(trip.Id, CancellationToken.None))
            .ReturnsAsync(new[] { candidate });
        Mock<ITripPreferenceRepository> preferences = new(MockBehavior.Strict);
        preferences.Setup(repository => repository.ListForUserAsync(
                trip.Id,
                trip.OwnerUserId,
                CancellationToken.None))
            .ReturnsAsync(new[] { preference });
        Mock<IParkRepository> parks = new(MockBehavior.Strict);
        parks.Setup(repository => repository.GetByIdsAsync(
                It.Is<IEnumerable<string>>(ids => ids.SequenceEqual(new[] { park.Id })),
                CancellationToken.None))
            .ReturnsAsync(new[] { park });
        Mock<IParkItemRepository> parkItems = new(MockBehavior.Strict);
        parkItems.Setup(repository => repository.GetVisibleOpenAttractionsByParkIdsAsync(
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { park.Id })),
                CancellationToken.None))
            .ReturnsAsync(new[] { parkItem });
        Mock<IImageRepository> images = new(MockBehavior.Strict);
        images.Setup(repository => repository.GetMainImageIdsByOwnersAsync(
                ImageOwnerType.ParkItem,
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { parkItem.Id })),
                ImageCategory.ParkItem,
                true,
                CancellationToken.None))
            .ReturnsAsync(new Dictionary<string, string> { [parkItem.Id] = "image-1" });
        Mock<ITripChildMutationLeaseRepository> leases = new(MockBehavior.Strict);
        TripPreferenceService service = CreateService(
            trips,
            candidates,
            preferences,
            parks,
            parkItems,
            images,
            leases);

        AmusementPark.Application.Errors.ApplicationResult<TripPreferenceBoardResult> result =
            await service.GetAsync(trip.OwnerUserId, trip.Id.Value, CancellationToken.None);

        Assert.True(result.IsSuccess);
        TripItemPreferenceResult item = Assert.Single(result.Value!.Items);
        Assert.Equal("Parc test", item.ParkName);
        Assert.Equal("Grand huit", item.ParkItemName);
        Assert.Equal("image-1", item.MainImageId);
        Assert.Equal(TripItemPreferenceLevel.MustDo, item.Level);
        Assert.True(result.Value.CanVote);
        trips.VerifyAll();
        candidates.VerifyAll();
        preferences.VerifyAll();
        parks.VerifyAll();
        parkItems.VerifyAll();
        images.VerifyAll();
        leases.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SetAsync_WhenItemIsOutsideCandidateParks_ShouldRejectWithoutWriting()
    {
        DateTime nowUtc = new(2027, 3, 4, 10, 0, 0, DateTimeKind.Utc);
        TripPlan trip = CreateTrip(nowUtc);
        TripMember owner = Assert.Single(trip.Members);
        TripParkCandidate candidate = CreateCandidate(trip, owner, nowUtc);
        TripChildMutationLease lease = new(
            "preference-operation",
            owner.Id,
            trip.ChildMutationEpoch,
            1,
            nowUtc.AddMinutes(1));
        Mock<ITripPlanRepository> trips = new(MockBehavior.Strict);
        trips.Setup(repository => repository.GetAccessibleAsync(
                trip.OwnerUserId,
                trip.Id,
                CancellationToken.None))
            .ReturnsAsync(trip);
        Mock<ITripParkCandidateRepository> candidates = new(MockBehavior.Strict);
        candidates.Setup(repository => repository.ListAsync(trip.Id, CancellationToken.None))
            .ReturnsAsync(new[] { candidate });
        Mock<ITripPreferenceRepository> preferences = new(MockBehavior.Strict);
        Mock<IParkRepository> parks = new(MockBehavior.Strict);
        parks.Setup(repository => repository.GetByIdsAsync(
                It.IsAny<IEnumerable<string>>(),
                CancellationToken.None))
            .ReturnsAsync(new[]
            {
                new Park { Id = "park-1", Name = "Parc test", IsVisible = true },
            });
        Mock<IParkItemRepository> parkItems = new(MockBehavior.Strict);
        parkItems.Setup(repository => repository.GetVisibleOpenAttractionsByParkIdsAsync(
                It.IsAny<IReadOnlyCollection<string>>(),
                CancellationToken.None))
            .ReturnsAsync(Array.Empty<ParkItem>());
        Mock<IImageRepository> images = new(MockBehavior.Strict);
        Mock<ITripChildMutationLeaseRepository> leases = new(MockBehavior.Strict);
        leases.Setup(repository => repository.TryAcquireAccessibleAsync(
                trip.Id,
                trip.OwnerUserId,
                owner.Id,
                trip.Version,
                trip.ChildMutationEpoch,
                It.IsAny<string>(),
                CancellationToken.None))
            .ReturnsAsync(lease);
        leases.Setup(repository => repository.ReleaseAsync(trip.Id, lease, CancellationToken.None))
            .Returns(Task.CompletedTask);
        TripPreferenceService service = CreateService(
            trips,
            candidates,
            preferences,
            parks,
            parkItems,
            images,
            leases);

        AmusementPark.Application.Errors.ApplicationResult<TripPreferenceBoardResult> result =
            await service.SetAsync(
                trip.OwnerUserId,
                trip.Id.Value,
                trip.Version,
                new[]
                {
                    new TripItemPreferenceInput(
                        "outside-item",
                        null,
                        TripItemPreferenceLevel.MustDo,
                        null),
                },
                CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, error => error.Code == "trip.preference.item-not-available");
        preferences.VerifyNoOtherCalls();
        images.VerifyNoOtherCalls();
        leases.VerifyAll();
    }

    [Fact]
    public async Task SetAsync_WhenSeveralChoicesAreValid_ShouldPersistTheWholeBatch()
    {
        DateTime nowUtc = new(2027, 3, 4, 10, 0, 0, DateTimeKind.Utc);
        TripPlan trip = CreateTrip(nowUtc);
        TripMember owner = Assert.Single(trip.Members);
        TripParkCandidate candidate = CreateCandidate(trip, owner, nowUtc);
        TripChildMutationLease lease = new(
            "preference-operation",
            owner.Id,
            trip.ChildMutationEpoch,
            1,
            nowUtc.AddMinutes(1));
        Park park = new()
        {
            Id = "park-1",
            Name = "Parc test",
            IsVisible = true,
            Status = ParkStatus.Operating,
        };
        ParkItem firstItem = new()
        {
            Id = "item-1",
            ParkId = park.Id,
            Name = "Grand huit",
            Category = ParkItemCategory.Attraction,
            IsVisible = true,
        };
        ParkItem secondItem = new()
        {
            Id = "item-2",
            ParkId = park.Id,
            Name = "Tour",
            Category = ParkItemCategory.Attraction,
            IsVisible = true,
        };
        List<TripItemPreference> stored = new();
        Mock<ITripPlanRepository> trips = new(MockBehavior.Strict);
        trips.Setup(repository => repository.GetAccessibleAsync(
                trip.OwnerUserId,
                trip.Id,
                CancellationToken.None))
            .ReturnsAsync(trip);
        Mock<ITripParkCandidateRepository> candidates = new(MockBehavior.Strict);
        candidates.Setup(repository => repository.ListAsync(trip.Id, CancellationToken.None))
            .ReturnsAsync(new[] { candidate });
        Mock<ITripPreferenceRepository> preferences = new(MockBehavior.Strict);
        preferences.Setup(repository => repository.ListForUserAsync(
                trip.Id,
                trip.OwnerUserId,
                CancellationToken.None))
            .ReturnsAsync(() => stored.ToArray());
        preferences.Setup(repository => repository.CreateAsync(
                It.IsAny<TripItemPreference>(),
                lease,
                It.IsAny<TripActivityWrite?>(),
                CancellationToken.None))
            .Returns((TripItemPreference preference, TripChildMutationLease _, TripActivityWrite? _, CancellationToken _) =>
            {
                stored.Add(preference);
                return Task.FromResult(new TripItemPreferenceWriteResult(
                    TripChildWriteOutcome.Success,
                    preference,
                    preference.Version));
            });
        Mock<IParkRepository> parks = new(MockBehavior.Strict);
        parks.Setup(repository => repository.GetByIdsAsync(
                It.IsAny<IEnumerable<string>>(),
                CancellationToken.None))
            .ReturnsAsync(new[] { park });
        Mock<IParkItemRepository> parkItems = new(MockBehavior.Strict);
        parkItems.Setup(repository => repository.GetVisibleOpenAttractionsByParkIdsAsync(
                It.IsAny<IReadOnlyCollection<string>>(),
                CancellationToken.None))
            .ReturnsAsync(new[] { firstItem, secondItem });
        Mock<IImageRepository> images = new(MockBehavior.Strict);
        images.Setup(repository => repository.GetMainImageIdsByOwnersAsync(
                ImageOwnerType.ParkItem,
                It.IsAny<IReadOnlyCollection<string>>(),
                ImageCategory.ParkItem,
                true,
                CancellationToken.None))
            .ReturnsAsync(new Dictionary<string, string>());
        Mock<ITripChildMutationLeaseRepository> leases = new(MockBehavior.Strict);
        leases.Setup(repository => repository.TryAcquireAccessibleAsync(
                trip.Id,
                trip.OwnerUserId,
                owner.Id,
                trip.Version,
                trip.ChildMutationEpoch,
                It.IsAny<string>(),
                CancellationToken.None))
            .ReturnsAsync(lease);
        leases.Setup(repository => repository.ReleaseAsync(trip.Id, lease, CancellationToken.None))
            .Returns(Task.CompletedTask);
        Mock<TimeProvider> clock = new(MockBehavior.Strict);
        clock.Setup(provider => provider.GetUtcNow()).Returns(new DateTimeOffset(nowUtc));
        TripPreferenceService service = CreateService(
            trips,
            candidates,
            preferences,
            parks,
            parkItems,
            images,
            leases,
            clock.Object);

        AmusementPark.Application.Errors.ApplicationResult<TripPreferenceBoardResult> result =
            await service.SetAsync(
                trip.OwnerUserId,
                trip.Id.Value,
                trip.Version,
                new[]
                {
                    new TripItemPreferenceInput(
                        firstItem.Id,
                        null,
                        TripItemPreferenceLevel.MustDo,
                        TripItemPreferenceReason.Sensations),
                    new TripItemPreferenceInput(
                        secondItem.Id,
                        null,
                        TripItemPreferenceLevel.WantToDo,
                        null),
                },
                CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, stored.Count);
        Assert.Collection(
            result.Value!.Items,
            item => Assert.Equal(TripItemPreferenceLevel.MustDo, item.Level),
            item => Assert.Equal(TripItemPreferenceLevel.WantToDo, item.Level));
        preferences.Verify(repository => repository.CreateAsync(
            It.IsAny<TripItemPreference>(),
            lease,
            It.IsAny<TripActivityWrite?>(),
            CancellationToken.None), Times.Exactly(2));
        leases.VerifyAll();
    }

    [Fact]
    public async Task SetAsync_WhenTheBatchPartiallyCommits_ShouldPublishTheCommittedCount()
    {
        DateTime nowUtc = new(2027, 3, 4, 10, 0, 0, DateTimeKind.Utc);
        TripPlan trip = CreateTrip(nowUtc);
        TripMember owner = Assert.Single(trip.Members);
        TripParkCandidate candidate = CreateCandidate(trip, owner, nowUtc);
        TripChildMutationLease lease = new(
            "preference-operation",
            owner.Id,
            trip.ChildMutationEpoch,
            3,
            nowUtc.AddMinutes(1));
        Park park = new()
        {
            Id = "park-1",
            Name = "Parc test",
            IsVisible = true,
            Status = ParkStatus.Operating,
        };
        ParkItem[] items =
        {
            new()
            {
                Id = "item-1",
                ParkId = park.Id,
                Name = "Grand huit",
                Category = ParkItemCategory.Attraction,
                IsVisible = true,
            },
            new()
            {
                Id = "item-2",
                ParkId = park.Id,
                Name = "Tour",
                Category = ParkItemCategory.Attraction,
                IsVisible = true,
            },
        };
        Mock<ITripPlanRepository> trips = new(MockBehavior.Strict);
        trips.Setup(repository => repository.GetAccessibleAsync(
                trip.OwnerUserId,
                trip.Id,
                CancellationToken.None))
            .ReturnsAsync(trip);
        Mock<ITripParkCandidateRepository> candidates = new(MockBehavior.Strict);
        candidates.Setup(repository => repository.ListAsync(trip.Id, CancellationToken.None))
            .ReturnsAsync(new[] { candidate });
        Mock<ITripPreferenceRepository> preferences = new(MockBehavior.Strict);
        preferences.Setup(repository => repository.ListForUserAsync(
                trip.Id,
                trip.OwnerUserId,
                CancellationToken.None))
            .ReturnsAsync(Array.Empty<TripItemPreference>());
        int writeNumber = 0;
        preferences.Setup(repository => repository.CreateAsync(
                It.IsAny<TripItemPreference>(),
                lease,
                It.Is<TripActivityWrite?>(activity =>
                    activity != null
                    && activity.Kind == TripActivityKind.PreferencesUpdated
                    && activity.AffectedCount == 1),
                CancellationToken.None))
            .Returns((TripItemPreference preference, TripChildMutationLease _, TripActivityWrite? _, CancellationToken _) =>
            {
                writeNumber++;
                return Task.FromResult(writeNumber == 1
                    ? new TripItemPreferenceWriteResult(
                        TripChildWriteOutcome.Success,
                        preference,
                        preference.Version)
                    : new TripItemPreferenceWriteResult(TripChildWriteOutcome.Conflict, null, 4));
            });
        Mock<IParkRepository> parks = new(MockBehavior.Strict);
        parks.Setup(repository => repository.GetByIdsAsync(
                It.IsAny<IEnumerable<string>>(),
                CancellationToken.None))
            .ReturnsAsync(new[] { park });
        Mock<IParkItemRepository> parkItems = new(MockBehavior.Strict);
        parkItems.Setup(repository => repository.GetVisibleOpenAttractionsByParkIdsAsync(
                It.IsAny<IReadOnlyCollection<string>>(),
                CancellationToken.None))
            .ReturnsAsync(items);
        Mock<IImageRepository> images = new(MockBehavior.Strict);
        Mock<ITripChildMutationLeaseRepository> leases = new(MockBehavior.Strict);
        leases.Setup(repository => repository.TryAcquireAccessibleAsync(
                trip.Id,
                trip.OwnerUserId,
                owner.Id,
                trip.Version,
                trip.ChildMutationEpoch,
                It.IsAny<string>(),
                CancellationToken.None))
            .ReturnsAsync(lease);
        leases.Setup(repository => repository.ReleaseAsync(trip.Id, lease, CancellationToken.None))
            .Returns(Task.CompletedTask);
        Mock<ITripAuditWriter> writer = new(MockBehavior.Strict);
        writer.Setup(port => port.AppendAsync(
                It.Is<TripActivityWrite>(activity =>
                    activity.Kind == TripActivityKind.PreferencesUpdated
                    && activity.AffectedCount == 1
                    && activity.OperationKey == TripActivityRecorder.ChildOperationKey(
                        TripActivityKind.PreferencesUpdated,
                        lease)),
                CancellationToken.None))
            .ReturnsAsync(true);
        Mock<TimeProvider> clock = new(MockBehavior.Strict);
        clock.Setup(provider => provider.GetUtcNow()).Returns(new DateTimeOffset(nowUtc));
        TripPreferenceService service = CreateService(
            trips,
            candidates,
            preferences,
            parks,
            parkItems,
            images,
            leases,
            clock.Object,
            new TripActivityRecorder(writer.Object, clock.Object));

        AmusementPark.Application.Errors.ApplicationResult<TripPreferenceBoardResult> result =
            await service.SetAsync(
                trip.OwnerUserId,
                trip.Id.Value,
                trip.Version,
                items.Select(item => new TripItemPreferenceInput(
                    item.Id,
                    null,
                    TripItemPreferenceLevel.WantToDo,
                    null)).ToArray(),
                CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(2, writeNumber);
        writer.VerifyAll();
        images.VerifyNoOtherCalls();
        leases.VerifyAll();
    }

    private static TripPreferenceService CreateService(
        Mock<ITripPlanRepository> trips,
        Mock<ITripParkCandidateRepository> candidates,
        Mock<ITripPreferenceRepository> preferences,
        Mock<IParkRepository> parks,
        Mock<IParkItemRepository> parkItems,
        Mock<IImageRepository> images,
        Mock<ITripChildMutationLeaseRepository> leases,
        TimeProvider? timeProvider = null,
        TripActivityRecorder? activityRecorder = null)
    {
        return new TripPreferenceService(
            trips.Object,
            preferences.Object,
            new TripEligibleItemReader(
                candidates.Object,
                parks.Object,
                parkItems.Object),
            images.Object,
            new TripChildMutationExecutor(
                leases.Object,
                NullLogger<TripChildMutationExecutor>.Instance),
            timeProvider,
            activityRecorder);
    }

    private static TripPlan CreateTrip(DateTime nowUtc)
    {
        return TripPlan.Create(
            TripPlanId.New(),
            "owner-1",
            "Voyage test",
            TripDateProposal.None(),
            null,
            nowUtc);
    }

    private static TripParkCandidate CreateCandidate(
        TripPlan trip,
        TripMember owner,
        DateTime nowUtc)
    {
        return TripParkCandidate.Create(
            TripParkCandidateId.New(),
            trip.Id,
            "park-1",
            Array.Empty<DateOnly>(),
            TripParkCandidateSource.Manual,
            null,
            null,
            owner.Id,
            TripParkCandidate.SortPositionStep,
            nowUtc);
    }
}
