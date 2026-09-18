using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkOpeningHours.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Trips.Models;
using AmusementPark.Application.Features.Trips.Ports;
using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.Application.Features.Trips.Services;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Trips;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Trips;

public sealed class TripProgramCoherenceServiceTests
{
    [Fact]
    public async Task GetAsync_ShouldExposeNamedOfficialEvidenceWithoutChangingTheProgram()
    {
        DateTime nowUtc = new DateTime(2027, 4, 1, 10, 0, 0, DateTimeKind.Utc);
        DateOnly visitDate = new DateOnly(2027, 5, 3);
        TripPlan trip = TripPlan.Create(
            TripPlanId.New(),
            "owner-1",
            "Voyage de printemps",
            TripDateProposal.Fixed(visitDate),
            "Europe/Paris",
            nowUtc);
        TripMember owner = Assert.Single(trip.Members);
        TripParkCandidate candidate = TripParkCandidate.Restore(
            TripParkCandidateId.New(),
            trip.Id,
            "park-technical-id",
            new[] { visitDate },
            TripParkCandidateSource.Manual,
            TripParkCandidateState.Selected,
            null,
            null,
            owner.Id,
            TripParkCandidate.SortPositionStep,
            1,
            nowUtc,
            nowUtc);
        TripDayPlan day = TripDayPlan.Create(
            TripDayPlanId.New(),
            trip.Id,
            visitDate,
            candidate.Id,
            candidate.ParkId,
            null,
            null,
            Array.Empty<TripDayBlock>(),
            nowUtc);
        Park park = new Park
        {
            Id = candidate.ParkId,
            Name = "Parc des preuves",
            IsVisible = true,
            Status = ParkStatus.Operating,
        };
        ParkOpeningHoursSchedule schedule = new ParkOpeningHoursSchedule
        {
            ParkId = park.Id,
            TimeZoneId = "Europe/Paris",
            SourceUrl = "https://example.com/official-hours",
            LastVerifiedAtUtc = nowUtc,
            RegularRules = new List<ParkOpeningHoursRule>
            {
                new ParkOpeningHoursRule
                {
                    StartDate = visitDate,
                    EndDate = visitDate,
                    DaysOfWeek = new List<DayOfWeek> { visitDate.DayOfWeek },
                    TimeRanges = new List<ParkOpeningHoursTimeRange>
                    {
                        new ParkOpeningHoursTimeRange
                        {
                            OpensAt = new TimeOnly(9, 0),
                            ClosesAt = new TimeOnly(18, 0),
                        },
                    },
                },
            },
        };
        Mock<ITripPlanRepository> plans = new Mock<ITripPlanRepository>(MockBehavior.Strict);
        plans.Setup(repository => repository.GetAccessibleAsync(
                trip.OwnerUserId,
                trip.Id,
                CancellationToken.None))
            .ReturnsAsync(trip);
        plans.SetupSequence(repository => repository.GetProgramReadSequenceAsync(
                trip.Id,
                CancellationToken.None))
            .ReturnsAsync(1)
            .ReturnsAsync(1);
        Mock<ITripParkCandidateRepository> candidates = new Mock<ITripParkCandidateRepository>(MockBehavior.Strict);
        candidates.Setup(repository => repository.ListAsync(trip.Id, CancellationToken.None))
            .ReturnsAsync(new[] { candidate });
        Mock<ITripDayPlanRepository> days = new Mock<ITripDayPlanRepository>(MockBehavior.Strict);
        days.Setup(repository => repository.ListAsync(trip.Id, CancellationToken.None))
            .ReturnsAsync(new[] { day });
        Mock<IParkRepository> parks = new Mock<IParkRepository>(MockBehavior.Strict);
        parks.Setup(repository => repository.GetByIdsAsync(
                It.IsAny<IEnumerable<string>>(),
                CancellationToken.None))
            .ReturnsAsync(new[] { park });
        Mock<IParkOpeningHoursRepository> openingHours = new Mock<IParkOpeningHoursRepository>(MockBehavior.Strict);
        openingHours.Setup(repository => repository.GetByParkIdsAsync(
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { park.Id })),
                CancellationToken.None))
            .ReturnsAsync(new[] { schedule });
        Mock<ITripItemDecisionRepository> decisions = new Mock<ITripItemDecisionRepository>(MockBehavior.Strict);
        decisions.Setup(repository => repository.ListAsync(trip.Id, CancellationToken.None))
            .ReturnsAsync(Array.Empty<TripItemDecision>());
        Mock<ITripPreferenceRepository> preferences = new Mock<ITripPreferenceRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> parkItems = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<TimeProvider> timeProvider = new Mock<TimeProvider>(MockBehavior.Strict);
        timeProvider.Setup(provider => provider.GetUtcNow()).Returns(new DateTimeOffset(nowUtc));
        TripProgramResultFactory programFactory = new TripProgramResultFactory(
            plans.Object,
            candidates.Object,
            days.Object,
            parks.Object);
        TripProgramCoherenceService service = new TripProgramCoherenceService(
            plans.Object,
            programFactory,
            parks.Object,
            openingHours.Object,
            decisions.Object,
            preferences.Object,
            parkItems.Object,
            new TripProgramEvidenceBuilder(new ParkOpeningHoursCalendarBuilder()),
            new TripProgramAttractionFactBuilder(),
            new TripProgramCoherenceEvaluator(),
            new TripProgramCoherenceIssueMapper(),
            timeProvider.Object);

        AmusementPark.Application.Errors.ApplicationResult<TripProgramCoherenceResult> result =
            await service.GetAsync(trip.OwnerUserId, trip.Id.Value, CancellationToken.None);

        Assert.True(result.IsSuccess);
        TripProgramCoherenceResult coherence = Assert.IsType<TripProgramCoherenceResult>(result.Value);
        TripProgramDayEvidenceResult evidence = Assert.Single(coherence.Days);
        Assert.Equal("Parc des preuves", evidence.ParkName);
        Assert.NotEqual(evidence.ParkId, evidence.ParkName);
        Assert.Equal(TripProgramOpeningState.Open, evidence.OpeningState);
        Assert.Equal(schedule.SourceUrl, evidence.OpeningHoursSourceUrl);
        Assert.Empty(coherence.Issues);
        Assert.Equal(TripParkCandidateState.Selected, candidate.State);
        plans.VerifyAll();
        candidates.VerifyAll();
        days.VerifyAll();
        parks.Verify(repository => repository.GetByIdsAsync(
            It.IsAny<IEnumerable<string>>(),
            CancellationToken.None), Times.Once);
        openingHours.VerifyAll();
        decisions.VerifyAll();
        preferences.VerifyNoOtherCalls();
        parkItems.VerifyNoOtherCalls();
        timeProvider.VerifyAll();
    }

    [Fact]
    public async Task GetAsync_WhenDecisionParentIsNoLongerACandidate_ShouldResolveItsParkWithoutFalseAlert()
    {
        DateTime nowUtc = new DateTime(2027, 4, 1, 10, 0, 0, DateTimeKind.Utc);
        TripPlan trip = TripPlan.Create(
            TripPlanId.New(),
            "owner-1",
            "Voyage partagé",
            TripDateProposal.Range(new DateOnly(2027, 5, 1), new DateOnly(2027, 5, 5)),
            "Europe/Paris",
            nowUtc);
        Park parentPark = new Park
        {
            Id = "removed-candidate-park",
            Name = "Parc toujours public",
            IsVisible = true,
            Status = ParkStatus.Operating,
        };
        ParkItem item = new ParkItem
        {
            Id = "retained-item",
            ParkId = parentPark.Id,
            Name = "Attraction conservée",
            Category = ParkItemCategory.Attraction,
            IsVisible = true,
            AttractionDetails = new AttractionDetails { Status = "Operating" },
        };
        TripItemDecision decision = TripItemDecision.Create(
            TripItemDecisionId.New(),
            trip.Id,
            item.Id,
            TripItemDecisionStatus.Retained,
            "Le groupe la conserve.",
            trip.OwnerUserId,
            nowUtc);
        Mock<ITripPlanRepository> plans = new Mock<ITripPlanRepository>(MockBehavior.Strict);
        plans.Setup(repository => repository.GetAccessibleAsync(
                trip.OwnerUserId,
                trip.Id,
                CancellationToken.None))
            .ReturnsAsync(trip);
        plans.SetupSequence(repository => repository.GetProgramReadSequenceAsync(
                trip.Id,
                CancellationToken.None))
            .ReturnsAsync(1)
            .ReturnsAsync(1);
        Mock<ITripParkCandidateRepository> candidates = new Mock<ITripParkCandidateRepository>(MockBehavior.Strict);
        candidates.Setup(repository => repository.ListAsync(trip.Id, CancellationToken.None))
            .ReturnsAsync(Array.Empty<TripParkCandidate>());
        Mock<ITripDayPlanRepository> days = new Mock<ITripDayPlanRepository>(MockBehavior.Strict);
        days.Setup(repository => repository.ListAsync(trip.Id, CancellationToken.None))
            .ReturnsAsync(Array.Empty<TripDayPlan>());
        Mock<IParkRepository> parks = new Mock<IParkRepository>(MockBehavior.Strict);
        parks.Setup(repository => repository.GetByIdsAsync(
                It.Is<IEnumerable<string>>(ids => ids.SequenceEqual(new[] { parentPark.Id })),
                CancellationToken.None))
            .ReturnsAsync(new[] { parentPark });
        Mock<IParkOpeningHoursRepository> openingHours = new Mock<IParkOpeningHoursRepository>(MockBehavior.Strict);
        Mock<ITripItemDecisionRepository> decisions = new Mock<ITripItemDecisionRepository>(MockBehavior.Strict);
        decisions.Setup(repository => repository.ListAsync(trip.Id, CancellationToken.None))
            .ReturnsAsync(new[] { decision });
        Mock<ITripPreferenceRepository> preferences = new Mock<ITripPreferenceRepository>(MockBehavior.Strict);
        preferences.Setup(repository => repository.SummarizeAsync(
                trip.Id,
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { trip.OwnerUserId })),
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { item.Id })),
                CancellationToken.None))
            .ReturnsAsync(Array.Empty<TripPreferenceCount>());
        Mock<IParkItemRepository> parkItems = new Mock<IParkItemRepository>(MockBehavior.Strict);
        parkItems.Setup(repository => repository.GetByIdsAsync(
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { item.Id })),
                CancellationToken.None))
            .ReturnsAsync(new[] { item });
        Mock<TimeProvider> timeProvider = new Mock<TimeProvider>(MockBehavior.Strict);
        timeProvider.Setup(provider => provider.GetUtcNow()).Returns(new DateTimeOffset(nowUtc));
        TripProgramResultFactory programFactory = new TripProgramResultFactory(
            plans.Object,
            candidates.Object,
            days.Object,
            parks.Object);
        TripProgramCoherenceService service = new TripProgramCoherenceService(
            plans.Object,
            programFactory,
            parks.Object,
            openingHours.Object,
            decisions.Object,
            preferences.Object,
            parkItems.Object,
            new TripProgramEvidenceBuilder(new ParkOpeningHoursCalendarBuilder()),
            new TripProgramAttractionFactBuilder(),
            new TripProgramCoherenceEvaluator(),
            new TripProgramCoherenceIssueMapper(),
            timeProvider.Object);

        AmusementPark.Application.Errors.ApplicationResult<TripProgramCoherenceResult> result =
            await service.GetAsync(trip.OwnerUserId, trip.Id.Value, CancellationToken.None);

        Assert.True(result.IsSuccess);
        TripProgramCoherenceResult coherence = Assert.IsType<TripProgramCoherenceResult>(result.Value);
        Assert.DoesNotContain(coherence.Issues, issue =>
            issue.Code == TripProgramCoherenceCode.AttractionUnavailable);
        parks.VerifyAll();
        openingHours.VerifyNoOtherCalls();
        decisions.VerifyAll();
        preferences.VerifyAll();
        parkItems.VerifyAll();
    }
}
