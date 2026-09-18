using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Trips.Models;
using AmusementPark.Application.Features.Trips.Ports;
using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.Application.Features.Trips.Services;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Trips;
using AmusementPark.Core.Domain.Users;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Trips;

public sealed class TripPreferenceSummaryServiceTests
{
    [Fact]
    public async Task SetDecisionAsync_WhenParticipantCannotEditProgram_ShouldRefuseBeforeTakingALease()
    {
        DateTime nowUtc = new(2027, 3, 4, 10, 0, 0, DateTimeKind.Utc);
        TripPlan trip = CreateFourMemberTrip(nowUtc);
        Mock<ITripPlanRepository> plans = new(MockBehavior.Strict);
        plans.Setup(repository => repository.GetAccessibleAsync(
                "member-2",
                trip.Id,
                CancellationToken.None))
            .ReturnsAsync(trip);
        Mock<ITripPreferenceRepository> preferences = new(MockBehavior.Strict);
        Mock<ITripItemDecisionRepository> decisions = new(MockBehavior.Strict);
        Mock<ITripParkCandidateRepository> candidates = new(MockBehavior.Strict);
        Mock<IParkRepository> parks = new(MockBehavior.Strict);
        Mock<IParkItemRepository> parkItems = new(MockBehavior.Strict);
        Mock<IImageRepository> images = new(MockBehavior.Strict);
        Mock<IUserRepository> users = new(MockBehavior.Strict);
        Mock<ITripChildMutationLeaseRepository> leases = new(MockBehavior.Strict);
        TripPreferenceSummaryService service = new(
            plans.Object,
            preferences.Object,
            decisions.Object,
            new TripEligibleItemReader(candidates.Object, parks.Object, parkItems.Object),
            images.Object,
            users.Object,
            new TripChildMutationExecutor(
                leases.Object,
                NullLogger<TripChildMutationExecutor>.Instance));

        AmusementPark.Application.Errors.ApplicationResult<TripPreferenceSummaryResult> result =
            await service.SetDecisionAsync(
                "member-2",
                trip.Id.Value,
                trip.Version,
                new TripItemDecisionInput(
                    "item-1",
                    null,
                    TripItemDecisionStatus.Review,
                    "Le groupe doit encore en parler."),
                CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("trip.decision.forbidden", Assert.Single(result.Errors).Code);
        plans.VerifyAll();
        preferences.VerifyNoOtherCalls();
        decisions.VerifyNoOtherCalls();
        candidates.VerifyNoOtherCalls();
        parks.VerifyNoOtherCalls();
        parkItems.VerifyNoOtherCalls();
        images.VerifyNoOtherCalls();
        users.VerifyNoOtherCalls();
        leases.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetAsync_WhenOneParticipantObjects_ShouldExposeConflictWithoutIndividualVotes()
    {
        DateTime nowUtc = new(2027, 3, 4, 10, 0, 0, DateTimeKind.Utc);
        TripPlan trip = CreateFourMemberTrip(nowUtc);
        TripMember owner = trip.Members.Single(member => member.UserId == trip.OwnerUserId);
        TripParkCandidate candidate = TripParkCandidate.Create(
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
            UpdatedAtUtc = nowUtc,
            AttractionDetails = new AttractionDetails
            {
                Status = "Operating",
                SourceUrl = "https://example.com/ride",
            },
        };
        TripItemDecision decision = TripItemDecision.Create(
            TripItemDecisionId.New(),
            trip.Id,
            parkItem.Id,
            TripItemDecisionStatus.Review,
            "Le groupe doit encore en parler.",
            trip.OwnerUserId,
            nowUtc);
        Mock<ITripPlanRepository> plans = new(MockBehavior.Strict);
        plans.Setup(repository => repository.GetAccessibleAsync(
                trip.OwnerUserId,
                trip.Id,
                CancellationToken.None))
            .ReturnsAsync(trip);
        Mock<ITripParkCandidateRepository> candidates = new(MockBehavior.Strict);
        candidates.Setup(repository => repository.ListAsync(trip.Id, CancellationToken.None))
            .ReturnsAsync(new[] { candidate });
        Mock<IParkRepository> parks = new(MockBehavior.Strict);
        parks.Setup(repository => repository.GetByIdsAsync(
                It.IsAny<IEnumerable<string>>(),
                CancellationToken.None))
            .ReturnsAsync(new[] { park });
        Mock<IParkItemRepository> parkItems = new(MockBehavior.Strict);
        parkItems.Setup(repository => repository.GetVisibleOpenAttractionsByParkIdsAsync(
                It.IsAny<IReadOnlyCollection<string>>(),
                CancellationToken.None))
            .ReturnsAsync(new[] { parkItem });
        Mock<ITripPreferenceRepository> preferences = new(MockBehavior.Strict);
        preferences.Setup(repository => repository.SummarizeAsync(
                trip.Id,
                It.Is<IReadOnlyCollection<string>>(ids => ids.Count == 4),
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { parkItem.Id })),
                CancellationToken.None))
            .ReturnsAsync(new[]
            {
                new TripPreferenceCount(parkItem.Id, TripItemPreferenceLevel.MustDo, 2),
                new TripPreferenceCount(parkItem.Id, TripItemPreferenceLevel.WantToDo, 1),
                new TripPreferenceCount(parkItem.Id, TripItemPreferenceLevel.NotForMe, 1),
            });
        Mock<ITripItemDecisionRepository> decisions = new(MockBehavior.Strict);
        decisions.Setup(repository => repository.ListAsync(trip.Id, CancellationToken.None))
            .ReturnsAsync(new[] { decision });
        Mock<IImageRepository> images = new(MockBehavior.Strict);
        images.Setup(repository => repository.GetMainImageIdsByOwnersAsync(
                ImageOwnerType.ParkItem,
                It.IsAny<IReadOnlyCollection<string>>(),
                ImageCategory.ParkItem,
                true,
                CancellationToken.None))
            .ReturnsAsync(new Dictionary<string, string> { [parkItem.Id] = "image-1" });
        User ownerUser = new() { Id = trip.OwnerUserId, PublicDisplayName = "CapitaineParc" };
        Mock<IUserRepository> users = new(MockBehavior.Strict);
        users.Setup(repository => repository.GetByIdsAsync(
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { trip.OwnerUserId })),
                CancellationToken.None))
            .ReturnsAsync(new[] { ownerUser });
        Mock<ITripChildMutationLeaseRepository> leases = new(MockBehavior.Strict);
        TripPreferenceSummaryService service = new(
            plans.Object,
            preferences.Object,
            decisions.Object,
            new TripEligibleItemReader(candidates.Object, parks.Object, parkItems.Object),
            images.Object,
            users.Object,
            new TripChildMutationExecutor(
                leases.Object,
                NullLogger<TripChildMutationExecutor>.Instance));

        AmusementPark.Application.Errors.ApplicationResult<TripPreferenceSummaryResult> result =
            await service.GetAsync(trip.OwnerUserId, trip.Id.Value, CancellationToken.None);

        Assert.True(result.IsSuccess);
        TripItemPreferenceSummaryResult item = Assert.Single(result.Value!.Items);
        Assert.Equal(TripPreferenceCompatibility.Conflict, item.Compatibility);
        Assert.Equal(1, item.NotForMeCount);
        Assert.False(item.IsGroupPriority);
        Assert.Null(item.OfficialStatusVerifiedAtUtc);
        Assert.Equal("CapitaineParc", item.Decision!.DecidedByDisplayName);
        Assert.True(result.Value.CanDecide);
        plans.VerifyAll();
        candidates.VerifyAll();
        parks.VerifyAll();
        parkItems.VerifyAll();
        preferences.VerifyAll();
        decisions.VerifyAll();
        images.VerifyAll();
        users.VerifyAll();
        leases.VerifyNoOtherCalls();
    }

    private static TripPlan CreateFourMemberTrip(DateTime nowUtc)
    {
        TripPlan created = TripPlan.Create(
            TripPlanId.New(),
            "owner-1",
            "Voyage test",
            TripDateProposal.None(),
            null,
            nowUtc);
        List<TripMember> members = created.Members.ToList();
        for (int index = 2; index <= 4; index++)
        {
            members.Add(TripMember.Restore(
                TripMemberId.New(),
                $"member-{index}",
                TripDelegatedRole.Participant,
                TripMembershipState.Active,
                nowUtc));
        }

        return TripPlan.Restore(
            created.Id,
            created.OwnerUserId,
            created.Title,
            created.DateProposal,
            created.DestinationTimeZoneId,
            created.Status,
            created.AccessScope,
            members,
            created.AdmissionClosureState,
            created.DeletionState,
            created.ChildMutationEpoch,
            created.CreatedAtUtc,
            created.UpdatedAtUtc,
            created.Version);
    }
}
