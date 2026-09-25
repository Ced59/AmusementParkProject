using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Passport.Commands;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Application.Features.Passport.Results;
using AmusementPark.Application.Features.Trips.Ports;
using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.Application.Features.Trips.Services;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Trips;
using AmusementPark.Core.Domain.Visits;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Trips;

public sealed class TripPassportTransitionServiceTests
{
    [Fact]
    public async Task GetAsync_ShouldOfferOnlyPastDaysAndExposeOnlyTheCallersPreference()
    {
        DateTime nowUtc = new(2027, 8, 22, 10, 0, 0, DateTimeKind.Utc);
        DateOnly pastDate = new(2027, 8, 20);
        DateOnly futureDate = new(2027, 8, 23);
        TripPlan trip = CreateTrip(nowUtc, pastDate, futureDate);
        TripMember owner = Assert.Single(trip.Members);
        TripDayPlan[] days =
        {
            CreateDay(trip, "park-1", pastDate, nowUtc),
            CreateDay(trip, "park-1", futureDate, nowUtc),
        };
        ParkItem preferredAttraction = CreateAttraction("item-1", "Grand huit");
        ParkItem neutralAttraction = CreateAttraction("item-2", "Tour");
        TripItemPreference preference = TripItemPreference.Create(
            TripItemPreferenceId.New(),
            trip.Id,
            owner.Id,
            trip.OwnerUserId,
            preferredAttraction.Id!,
            TripItemPreferenceLevel.MustDo,
            TripItemPreferenceReason.Sensations,
            nowUtc);

        Mock<ITripPlanRepository> trips = CreateTripRepository(trip);
        Mock<ITripParkCandidateRepository> candidates = CreateCandidateRepository(trip.Id);
        Mock<ITripDayPlanRepository> dayPlans = CreateDayRepository(trip.Id, days);
        Mock<IParkRepository> parks = CreateParkRepository();
        Mock<ITripPreferenceRepository> preferences = new(MockBehavior.Strict);
        preferences.Setup(repository => repository.ListForUserAsync(
                trip.Id,
                trip.OwnerUserId,
                CancellationToken.None))
            .ReturnsAsync(new[] { preference });
        Mock<IParkItemRepository> parkItems = new(MockBehavior.Strict);
        parkItems.Setup(repository => repository.GetByParkIdsAsync(
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { "park-1" })),
                false,
                CancellationToken.None))
            .ReturnsAsync(new[] { preferredAttraction, neutralAttraction });
        Mock<IImageRepository> images = new(MockBehavior.Strict);
        images.Setup(repository => repository.GetMainImageIdsByOwnersAsync(
                ImageOwnerType.ParkItem,
                It.Is<IReadOnlyCollection<string>>(ids => ids.Count == 2),
                ImageCategory.ParkItem,
                true,
                CancellationToken.None))
            .ReturnsAsync(new Dictionary<string, string>
            {
                [preferredAttraction.Id!] = "image-1",
            });
        Mock<IUserVisitRepository> visits = new(MockBehavior.Strict);
        visits.Setup(repository => repository.ListOwnedByExactDatesAsync(
                trip.OwnerUserId,
                It.Is<IReadOnlyCollection<DateOnly>>(dates => dates.SequenceEqual(new[]
                {
                    pastDate,
                    futureDate,
                })),
                CancellationToken.None))
            .ReturnsAsync(Array.Empty<Visit>());
        Mock<IRideOccurrenceRepository> rideOccurrences = new(MockBehavior.Strict);
        Mock<IPassportLocalDateResolver> dates = new(MockBehavior.Strict);
        dates.Setup(resolver => resolver.Resolve(nowUtc, "Europe/Paris"))
            .Returns(new DateOnly(2027, 8, 22));
        Mock<TimeProvider> clock = new(MockBehavior.Strict);
        clock.Setup(provider => provider.GetUtcNow()).Returns(new DateTimeOffset(nowUtc));
        TripPassportTransitionReader reader = new(
            trips.Object,
            new TripProgramResultFactory(
                trips.Object,
                candidates.Object,
                dayPlans.Object,
                parks.Object),
            preferences.Object,
            parkItems.Object,
            images.Object,
            visits.Object,
            rideOccurrences.Object,
            dates.Object,
            clock.Object);

        ApplicationResult<TripPassportTransitionResult> result = await reader.GetAsync(
            trip.OwnerUserId,
            trip.Id.Value,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(new DateOnly(2027, 8, 22), result.Value!.DestinationToday);
        TripPassportTransitionDayResult pastDay = result.Value.Days.Single(day => day.LocalDate == pastDate);
        Assert.True(pastDay.CanConfirm);
        Assert.Equal(2, pastDay.Attractions.Count);
        TripPassportTransitionItemResult preferred = pastDay.Attractions.Single(
            item => item.ParkItemId == preferredAttraction.Id);
        Assert.Equal(TripItemPreferenceLevel.MustDo, preferred.OwnPreference);
        Assert.Equal("image-1", preferred.MainImageId);
        TripPassportTransitionDayResult futureDay = result.Value.Days.Single(
            day => day.LocalDate == futureDate);
        Assert.False(futureDay.CanConfirm);
        Assert.Empty(futureDay.Attractions);
        trips.VerifyAll();
        candidates.VerifyAll();
        dayPlans.VerifyAll();
        parks.VerifyAll();
        preferences.VerifyAll();
        parkItems.VerifyAll();
        images.VerifyAll();
        visits.VerifyAll();
        rideOccurrences.VerifyAll();
        dates.VerifyAll();
        clock.VerifyAll();
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task GetAsync_WhenATransitionDraftExists_ShouldOnlyResumeAnUnfinishedRideBatch(
        bool rideBatchCompleted,
        bool expectedCanResume)
    {
        DateTime nowUtc = new(2027, 8, 22, 10, 0, 0, DateTimeKind.Utc);
        DateOnly visitDate = new(2027, 8, 20);
        TripPlan trip = CreateTrip(nowUtc, visitDate);
        TripDayPlan day = CreateDay(trip, "park-1", visitDate, nowUtc);
        ParkItem attraction = CreateAttraction("item-1", "Grand huit");
        Visit existingDraft = Visit.Create(
            VisitId.New(),
            trip.OwnerUserId,
            "park-1",
            VisitDate.ForDay(2027, 8, 20),
            "Europe/Paris",
            LocalServiceDayConvention.UserSelectedServiceDate,
            null,
            null,
            nowUtc);
        string rideOperationId = TripPassportTransitionOperationKeys.Rides(
            trip.Id.Value,
            trip.OwnerUserId,
            visitDate);

        Mock<ITripPlanRepository> trips = CreateTripRepository(trip);
        Mock<ITripParkCandidateRepository> candidates = CreateCandidateRepository(trip.Id);
        Mock<ITripDayPlanRepository> dayPlans = CreateDayRepository(trip.Id, new[] { day });
        Mock<IParkRepository> parks = CreateParkRepository();
        Mock<ITripPreferenceRepository> preferences = new(MockBehavior.Strict);
        preferences.Setup(repository => repository.ListForUserAsync(
                trip.Id,
                trip.OwnerUserId,
                CancellationToken.None))
            .ReturnsAsync(Array.Empty<TripItemPreference>());
        Mock<IParkItemRepository> parkItems = new(MockBehavior.Strict);
        parkItems.Setup(repository => repository.GetByParkIdsAsync(
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { "park-1" })),
                false,
                CancellationToken.None))
            .ReturnsAsync(new[] { attraction });
        Mock<IImageRepository> images = new(MockBehavior.Strict);
        images.Setup(repository => repository.GetMainImageIdsByOwnersAsync(
                ImageOwnerType.ParkItem,
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { attraction.Id! })),
                ImageCategory.ParkItem,
                true,
                CancellationToken.None))
            .ReturnsAsync(new Dictionary<string, string>());
        Mock<IUserVisitRepository> visits = new(MockBehavior.Strict);
        visits.Setup(repository => repository.ListOwnedByExactDatesAsync(
                trip.OwnerUserId,
                It.Is<IReadOnlyCollection<DateOnly>>(dates => dates.SequenceEqual(new[] { visitDate })),
                CancellationToken.None))
            .ReturnsAsync(new[] { existingDraft });
        visits.Setup(repository => repository.ListOwnedCreationOperationVisitIdsAsync(
                trip.OwnerUserId,
                It.Is<IReadOnlyCollection<string>>(operationIds =>
                    operationIds.Single().StartsWith("trip-passport-visit:", StringComparison.Ordinal)),
                CancellationToken.None))
            .ReturnsAsync(new[] { existingDraft.Id });
        Mock<IRideOccurrenceRepository> rideOccurrences = new(MockBehavior.Strict);
        rideOccurrences.Setup(repository => repository.ListBatchCreationOperationStatesAsync(
                trip.OwnerUserId,
                It.Is<IReadOnlyCollection<string>>(operationIds =>
                    operationIds.SequenceEqual(new[] { rideOperationId })),
                CancellationToken.None))
            .ReturnsAsync(new[]
            {
                new RideOccurrenceBatchCreationOperationState(
                    rideOperationId,
                    rideBatchCompleted,
                    new[] { attraction.Id! }),
            });
        Mock<IPassportLocalDateResolver> dates = new(MockBehavior.Strict);
        dates.Setup(resolver => resolver.Resolve(nowUtc, "Europe/Paris"))
            .Returns(new DateOnly(2027, 8, 22));
        Mock<TimeProvider> clock = new(MockBehavior.Strict);
        clock.Setup(provider => provider.GetUtcNow()).Returns(new DateTimeOffset(nowUtc));
        TripPassportTransitionReader reader = new(
            trips.Object,
            new TripProgramResultFactory(
                trips.Object,
                candidates.Object,
                dayPlans.Object,
                parks.Object),
            preferences.Object,
            parkItems.Object,
            images.Object,
            visits.Object,
            rideOccurrences.Object,
            dates.Object,
            clock.Object);

        ApplicationResult<TripPassportTransitionResult> result = await reader.GetAsync(
            trip.OwnerUserId,
            trip.Id.Value,
            CancellationToken.None);

        TripPassportTransitionDayResult transitionDay = Assert.Single(result.Value!.Days);
        Assert.Equal(expectedCanResume, transitionDay.CanResume);
        Assert.Equal(expectedCanResume, transitionDay.CanConfirm);
        Assert.Equal(existingDraft.Id.Value, transitionDay.ExistingVisitId);
        Assert.Equal(expectedCanResume, transitionDay.Attractions.Count == 1);
        if (expectedCanResume)
        {
            Assert.True(Assert.Single(transitionDay.Attractions).IsPreselected);
        }
        trips.VerifyAll();
        candidates.VerifyAll();
        dayPlans.VerifyAll();
        parks.VerifyAll();
        preferences.VerifyAll();
        parkItems.VerifyAll();
        images.VerifyAll();
        visits.VerifyAll();
        rideOccurrences.VerifyAll();
        dates.VerifyAll();
        clock.VerifyAll();
    }

    [Fact]
    public async Task ConfirmAsync_ShouldCreateAPrivateDraftAndOnlyAddExplicitlySelectedAttractions()
    {
        DateTime nowUtc = new(2027, 8, 22, 10, 0, 0, DateTimeKind.Utc);
        DateOnly visitDate = new(2027, 8, 20);
        TripPlan trip = CreateTrip(nowUtc, visitDate);
        TripDayPlan day = CreateDay(trip, "park-1", visitDate, nowUtc);
        ParkItem selectedAttraction = CreateAttraction("item-1", "Grand huit");
        ParkItem unselectedAttraction = CreateAttraction("item-2", "Tour prioritaire");
        VisitId visitId = VisitId.New();
        Visit createdVisit = Visit.Create(
            visitId,
            trip.OwnerUserId,
            "park-1",
            new VisitDate(2027, 8, 20, VisitDatePrecision.Day, false),
            "Europe/Paris",
            LocalServiceDayConvention.UserSelectedServiceDate,
            null,
            null,
            nowUtc);

        Mock<ITripPlanRepository> trips = CreateTripRepository(trip);
        Mock<ITripParkCandidateRepository> candidates = CreateCandidateRepository(trip.Id);
        Mock<ITripDayPlanRepository> dayPlans = CreateDayRepository(trip.Id, new[] { day });
        Mock<IParkRepository> parks = CreateParkRepository();
        Mock<IParkItemRepository> parkItems = new(MockBehavior.Strict);
        parkItems.Setup(repository => repository.GetByParkIdAsync(
                "park-1",
                false,
                CancellationToken.None))
            .ReturnsAsync(new[] { selectedAttraction, unselectedAttraction });
        Mock<IUserVisitRepository> visits = new(MockBehavior.Strict);
        visits.Setup(repository => repository.ListOwnedByExactDatesAsync(
                trip.OwnerUserId,
                It.Is<IReadOnlyCollection<DateOnly>>(dates => dates.SequenceEqual(new[] { visitDate })),
                CancellationToken.None))
            .ReturnsAsync(Array.Empty<Visit>());
        visits.Setup(repository => repository.ReleaseDeletedCreationOperationAsync(
                trip.OwnerUserId,
                It.Is<string>(operationId => operationId.StartsWith(
                    "trip-passport-visit:",
                    StringComparison.Ordinal)),
                CancellationToken.None))
            .Returns(Task.CompletedTask);
        visits.Setup(repository => repository.GetOwnedAsync(
                visitId,
                trip.OwnerUserId,
                CancellationToken.None))
            .ReturnsAsync(createdVisit);
        Mock<IPassportLocalDateResolver> dates = new(MockBehavior.Strict);
        dates.Setup(resolver => resolver.Resolve(nowUtc, "Europe/Paris"))
            .Returns(new DateOnly(2027, 8, 22));
        Mock<ICommandHandler<CreateVisitCommand, ApplicationResult<CreateVisitResult>>> createVisit =
            new(MockBehavior.Strict);
        createVisit.Setup(handler => handler.HandleAsync(
                It.Is<CreateVisitCommand>(command =>
                    command.UserId == trip.OwnerUserId
                    && command.ParkId == "park-1"
                    && command.Year == 2027
                    && command.Month == 8
                    && command.Day == 20
                    && command.Precision == VisitDatePrecision.Day
                    && command.ServiceDayConvention == LocalServiceDayConvention.UserSelectedServiceDate),
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<CreateVisitResult>.Success(new CreateVisitResult(
                new VisitResult(
                    visitId.Value,
                    "park-1",
                    new VisitDateResult(2027, 8, 20, VisitDatePrecision.Day, false),
                    "Europe/Paris",
                    LocalServiceDayConvention.UserSelectedServiceDate,
                    VisitStatus.Draft,
                    VisitPrivacy.Private,
                    null,
                    null,
                    1,
                    nowUtc,
                    nowUtc,
                    null),
                false)));
        Mock<ICommandHandler<AddRideOccurrencesBatchCommand,
            ApplicationResult<CreateRideOccurrencesResult>>> addRides = new(MockBehavior.Strict);
        addRides.Setup(handler => handler.HandleAsync(
                It.Is<AddRideOccurrencesBatchCommand>(command =>
                    command.Source == RideLogSource.TripTransition
                    && command.Items.Count == 1
                    && command.Items.Single()!.ParkItemId == selectedAttraction.Id
                    && command.Items.Single()!.Status == RideOccurrenceStatus.Completed
                    && command.Items.Single()!.ConfirmHistoricalConflict),
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<CreateRideOccurrencesResult>.Success(
                new CreateRideOccurrencesResult(Array.Empty<RideOccurrenceResult>(), false, false)));
        Mock<IRideOccurrenceRepository> rideOccurrences = new(MockBehavior.Strict);
        Mock<TimeProvider> clock = new(MockBehavior.Strict);
        clock.Setup(provider => provider.GetUtcNow()).Returns(new DateTimeOffset(nowUtc));
        TripPassportTransitionConfirmer confirmer = new(
            trips.Object,
            new TripProgramResultFactory(
                trips.Object,
                candidates.Object,
                dayPlans.Object,
                parks.Object),
            parkItems.Object,
            visits.Object,
            rideOccurrences.Object,
            dates.Object,
            createVisit.Object,
            addRides.Object,
            clock.Object);

        ApplicationResult<ConfirmTripPassportTransitionResult> result = await confirmer.ConfirmAsync(
            trip.OwnerUserId,
            trip.Id.Value,
            visitDate,
            new[] { selectedAttraction.Id! },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(visitId.Value, result.Value!.VisitId);
        createVisit.VerifyAll();
        addRides.VerifyAll();
        visits.VerifyAll();
        trips.VerifyAll();
        candidates.VerifyAll();
        dayPlans.VerifyAll();
        parks.VerifyAll();
        parkItems.VerifyAll();
        dates.VerifyAll();
        clock.VerifyAll();
    }

    [Fact]
    public async Task ConfirmAsync_WithoutSelectedAttractions_ShouldPersistACompletedEmptyBatch()
    {
        DateTime nowUtc = new(2027, 8, 22, 10, 0, 0, DateTimeKind.Utc);
        DateOnly visitDate = new(2027, 8, 20);
        TripPlan trip = CreateTrip(nowUtc, visitDate);
        TripDayPlan day = CreateDay(trip, "park-1", visitDate, nowUtc);
        VisitId visitId = VisitId.New();
        Visit createdVisit = Visit.Create(
            visitId,
            trip.OwnerUserId,
            "park-1",
            VisitDate.ForDay(2027, 8, 20),
            "Europe/Paris",
            LocalServiceDayConvention.UserSelectedServiceDate,
            null,
            null,
            nowUtc);

        Mock<ITripPlanRepository> trips = CreateTripRepository(trip);
        Mock<ITripParkCandidateRepository> candidates = CreateCandidateRepository(trip.Id);
        Mock<ITripDayPlanRepository> dayPlans = CreateDayRepository(trip.Id, new[] { day });
        Mock<IParkRepository> parks = CreateParkRepository();
        Mock<IParkItemRepository> parkItems = new(MockBehavior.Strict);
        parkItems.Setup(repository => repository.GetByParkIdAsync(
                "park-1",
                false,
                CancellationToken.None))
            .ReturnsAsync(Array.Empty<ParkItem>());
        Mock<IUserVisitRepository> visits = new(MockBehavior.Strict);
        visits.Setup(repository => repository.ListOwnedByExactDatesAsync(
                trip.OwnerUserId,
                It.Is<IReadOnlyCollection<DateOnly>>(dates => dates.SequenceEqual(new[] { visitDate })),
                CancellationToken.None))
            .ReturnsAsync(Array.Empty<Visit>());
        visits.Setup(repository => repository.ReleaseDeletedCreationOperationAsync(
                trip.OwnerUserId,
                It.Is<string>(operationId => operationId.StartsWith(
                    "trip-passport-visit:",
                    StringComparison.Ordinal)),
                CancellationToken.None))
            .Returns(Task.CompletedTask);
        visits.Setup(repository => repository.GetOwnedAsync(
                visitId,
                trip.OwnerUserId,
                CancellationToken.None))
            .ReturnsAsync(createdVisit);
        Mock<IRideOccurrenceRepository> rideOccurrences = new(MockBehavior.Strict);
        rideOccurrences.Setup(repository => repository.CompleteEmptyBatchCreationOperationAsync(
                trip.OwnerUserId,
                visitId,
                It.Is<string>(operationId => operationId.StartsWith(
                    "trip-passport-rides:",
                    StringComparison.Ordinal)),
                nowUtc,
                CancellationToken.None))
            .ReturnsAsync(true);
        Mock<IPassportLocalDateResolver> dates = new(MockBehavior.Strict);
        dates.Setup(resolver => resolver.Resolve(nowUtc, "Europe/Paris"))
            .Returns(new DateOnly(2027, 8, 22));
        Mock<ICommandHandler<CreateVisitCommand, ApplicationResult<CreateVisitResult>>> createVisit =
            new(MockBehavior.Strict);
        createVisit.Setup(handler => handler.HandleAsync(
                It.Is<CreateVisitCommand>(command => command.ParkId == "park-1"),
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<CreateVisitResult>.Success(new CreateVisitResult(
                new VisitResult(
                    visitId.Value,
                    "park-1",
                    new VisitDateResult(2027, 8, 20, VisitDatePrecision.Day, false),
                    "Europe/Paris",
                    LocalServiceDayConvention.UserSelectedServiceDate,
                    VisitStatus.Draft,
                    VisitPrivacy.Private,
                    null,
                    null,
                    1,
                    nowUtc,
                    nowUtc,
                    null),
                false)));
        Mock<ICommandHandler<AddRideOccurrencesBatchCommand,
            ApplicationResult<CreateRideOccurrencesResult>>> addRides = new(MockBehavior.Strict);
        Mock<TimeProvider> clock = new(MockBehavior.Strict);
        clock.Setup(provider => provider.GetUtcNow()).Returns(new DateTimeOffset(nowUtc));
        TripPassportTransitionConfirmer confirmer = new(
            trips.Object,
            new TripProgramResultFactory(
                trips.Object,
                candidates.Object,
                dayPlans.Object,
                parks.Object),
            parkItems.Object,
            visits.Object,
            rideOccurrences.Object,
            dates.Object,
            createVisit.Object,
            addRides.Object,
            clock.Object);

        ApplicationResult<ConfirmTripPassportTransitionResult> result = await confirmer.ConfirmAsync(
            trip.OwnerUserId,
            trip.Id.Value,
            visitDate,
            Array.Empty<string>(),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value!.AddedRideCount);
        rideOccurrences.VerifyAll();
        visits.VerifyAll();
        createVisit.VerifyAll();
        trips.VerifyAll();
        candidates.VerifyAll();
        dayPlans.VerifyAll();
        parks.VerifyAll();
        parkItems.VerifyAll();
        dates.VerifyAll();
        clock.VerifyAll();
    }

    [Fact]
    public async Task ConfirmAsync_WhenReservedAttractionIsNoLongerVisible_ShouldResumePersistedSelection()
    {
        DateTime nowUtc = new(2027, 8, 22, 10, 0, 0, DateTimeKind.Utc);
        DateOnly visitDate = new(2027, 8, 20);
        TripPlan trip = CreateTrip(nowUtc, visitDate);
        TripDayPlan day = CreateDay(trip, "park-1", visitDate, nowUtc);
        ParkItem selectedAttraction = CreateAttraction("item-1", "Grand huit");
        Visit existingDraft = Visit.Create(
            VisitId.New(),
            trip.OwnerUserId,
            "park-1",
            VisitDate.ForDay(2027, 8, 20),
            "Europe/Paris",
            LocalServiceDayConvention.UserSelectedServiceDate,
            null,
            null,
            nowUtc);

        Mock<ITripPlanRepository> trips = CreateTripRepository(trip);
        Mock<ITripParkCandidateRepository> candidates = CreateCandidateRepository(trip.Id);
        Mock<ITripDayPlanRepository> dayPlans = CreateDayRepository(trip.Id, new[] { day });
        Mock<IParkRepository> parks = CreateParkRepository();
        Mock<IParkItemRepository> parkItems = new(MockBehavior.Strict);
        parkItems.Setup(repository => repository.GetByParkIdAsync(
                "park-1",
                false,
                CancellationToken.None))
            .ReturnsAsync(Array.Empty<ParkItem>());
        Mock<IUserVisitRepository> visits = new(MockBehavior.Strict);
        visits.Setup(repository => repository.ListOwnedByExactDatesAsync(
                trip.OwnerUserId,
                It.Is<IReadOnlyCollection<DateOnly>>(dates => dates.SequenceEqual(new[] { visitDate })),
                CancellationToken.None))
            .ReturnsAsync(new[] { existingDraft });
        visits.Setup(repository => repository.ResolveExistingCreationAsync(
                It.Is<Visit>(visit => visit.ParkId == "park-1" && visit.Date == existingDraft.Date),
                It.Is<string>(operationId => operationId.StartsWith("trip-passport-visit:", StringComparison.Ordinal)),
                CancellationToken.None))
            .ReturnsAsync(new IdempotentVisitCreationResult(
                IdempotentVisitCreationStatus.Replayed,
                existingDraft));
        visits.Setup(repository => repository.GetOwnedAsync(
                existingDraft.Id,
                trip.OwnerUserId,
                CancellationToken.None))
            .ReturnsAsync(existingDraft);
        Mock<IPassportLocalDateResolver> dates = new(MockBehavior.Strict);
        dates.Setup(resolver => resolver.Resolve(nowUtc, "Europe/Paris"))
            .Returns(new DateOnly(2027, 8, 22));
        Mock<ICommandHandler<CreateVisitCommand, ApplicationResult<CreateVisitResult>>> createVisit =
            new(MockBehavior.Strict);
        createVisit.Setup(handler => handler.HandleAsync(
                It.Is<CreateVisitCommand>(command => command.ParkId == "park-1"),
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<CreateVisitResult>.Success(new CreateVisitResult(
                new VisitResult(
                    existingDraft.Id.Value,
                    existingDraft.ParkId,
                    new VisitDateResult(2027, 8, 20, VisitDatePrecision.Day, false),
                    existingDraft.TimeZoneId,
                    existingDraft.ServiceDayConvention,
                    existingDraft.Status,
                    existingDraft.Privacy,
                    null,
                    null,
                    existingDraft.Version,
                    existingDraft.CreatedAtUtc,
                    existingDraft.UpdatedAtUtc,
                    null),
                true)));
        Mock<ICommandHandler<AddRideOccurrencesBatchCommand,
            ApplicationResult<CreateRideOccurrencesResult>>> addRides = new(MockBehavior.Strict);
        addRides.Setup(handler => handler.HandleAsync(
                It.Is<AddRideOccurrencesBatchCommand>(command =>
                    command.VisitId == existingDraft.Id.Value
                    && command.Source == RideLogSource.TripTransition
                    && command.Items.Single()!.ParkItemId == selectedAttraction.Id),
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<CreateRideOccurrencesResult>.Success(
                new CreateRideOccurrencesResult(Array.Empty<RideOccurrenceResult>(), true, false)));
        Mock<IRideOccurrenceRepository> rideOccurrences = new(MockBehavior.Strict);
        rideOccurrences.Setup(repository => repository.ListBatchCreationOperationStatesAsync(
                trip.OwnerUserId,
                It.Is<IReadOnlyCollection<string>>(operationIds =>
                    operationIds.Single().StartsWith(
                        "trip-passport-rides:",
                        StringComparison.Ordinal)),
                CancellationToken.None))
            .ReturnsAsync(new[]
            {
                new RideOccurrenceBatchCreationOperationState(
                    TripPassportTransitionOperationKeys.Rides(
                        trip.Id.Value,
                        trip.OwnerUserId,
                        visitDate),
                    false,
                    new[] { selectedAttraction.Id! }),
            });
        Mock<TimeProvider> clock = new(MockBehavior.Strict);
        clock.Setup(provider => provider.GetUtcNow()).Returns(new DateTimeOffset(nowUtc));
        TripPassportTransitionConfirmer confirmer = new(
            trips.Object,
            new TripProgramResultFactory(
                trips.Object,
                candidates.Object,
                dayPlans.Object,
                parks.Object),
            parkItems.Object,
            visits.Object,
            rideOccurrences.Object,
            dates.Object,
            createVisit.Object,
            addRides.Object,
            clock.Object);

        ApplicationResult<ConfirmTripPassportTransitionResult> result = await confirmer.ConfirmAsync(
            trip.OwnerUserId,
            trip.Id.Value,
            visitDate,
            Array.Empty<string>(),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.WasReplayed);
        Assert.Equal(existingDraft.Id.Value, result.Value.VisitId);
        visits.VerifyAll();
        createVisit.VerifyAll();
        addRides.VerifyAll();
        rideOccurrences.VerifyAll();
        trips.VerifyAll();
        candidates.VerifyAll();
        dayPlans.VerifyAll();
        parks.VerifyAll();
        parkItems.VerifyAll();
        dates.VerifyAll();
        clock.VerifyAll();
    }

    private static TripPlan CreateTrip(DateTime nowUtc, params DateOnly[] dates)
    {
        return TripPlan.Create(
            TripPlanId.New(),
            "user-1",
            "Voyage test",
            TripDateProposal.Fixed(dates.Min(), dates.Max()),
            "Europe/Paris",
            nowUtc);
    }

    private static TripDayPlan CreateDay(
        TripPlan trip,
        string parkId,
        DateOnly localDate,
        DateTime nowUtc)
    {
        return TripDayPlan.Create(
            TripDayPlanId.New(),
            trip.Id,
            localDate,
            TripParkCandidateId.New(),
            parkId,
            null,
            null,
            Array.Empty<TripDayBlock>(),
            nowUtc);
    }

    private static ParkItem CreateAttraction(string id, string name)
    {
        return new ParkItem
        {
            Id = id,
            ParkId = "park-1",
            Name = name,
            Category = ParkItemCategory.Attraction,
            IsVisible = true,
        };
    }

    private static Mock<ITripPlanRepository> CreateTripRepository(TripPlan trip)
    {
        Mock<ITripPlanRepository> repository = new(MockBehavior.Strict);
        repository.Setup(item => item.GetAccessibleAsync(
                trip.OwnerUserId,
                trip.Id,
                CancellationToken.None))
            .ReturnsAsync(trip);
        repository.SetupSequence(item => item.GetProgramReadSequenceAsync(
                trip.Id,
                CancellationToken.None))
            .ReturnsAsync(1)
            .ReturnsAsync(1);
        return repository;
    }

    private static Mock<ITripParkCandidateRepository> CreateCandidateRepository(TripPlanId tripId)
    {
        Mock<ITripParkCandidateRepository> repository = new(MockBehavior.Strict);
        repository.Setup(item => item.ListAsync(tripId, CancellationToken.None))
            .ReturnsAsync(Array.Empty<TripParkCandidate>());
        return repository;
    }

    private static Mock<ITripDayPlanRepository> CreateDayRepository(
        TripPlanId tripId,
        IReadOnlyCollection<TripDayPlan> days)
    {
        Mock<ITripDayPlanRepository> repository = new(MockBehavior.Strict);
        repository.Setup(item => item.ListAsync(tripId, CancellationToken.None))
            .ReturnsAsync(days);
        return repository;
    }

    private static Mock<IParkRepository> CreateParkRepository()
    {
        Mock<IParkRepository> repository = new(MockBehavior.Strict);
        repository.Setup(item => item.GetByIdsAsync(
                It.Is<IEnumerable<string>>(ids => ids.SequenceEqual(new[] { "park-1" })),
                CancellationToken.None))
            .ReturnsAsync(new[]
            {
                new Park
                {
                    Id = "park-1",
                    Name = "Parc test",
                    IsVisible = true,
                    Status = ParkStatus.Operating,
                },
            });
        return repository;
    }
}
