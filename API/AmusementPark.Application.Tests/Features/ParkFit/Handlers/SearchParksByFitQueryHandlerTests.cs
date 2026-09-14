using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkFit;
using AmusementPark.Application.Features.ParkFit.Handlers;
using AmusementPark.Application.Features.ParkFit.Models;
using AmusementPark.Application.Features.ParkFit.Queries;
using AmusementPark.Application.Features.ParkFit.Results;
using AmusementPark.Application.Features.ParkFit.Services;
using AmusementPark.Application.Features.ParkFit.Validation;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkOpeningHours.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Core.Domain.ParkFit;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Localization;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.ParkFit.Handlers;

public sealed class SearchParksByFitQueryHandlerTests
{
    private static readonly DateTime EvaluationTimestamp =
        new DateTime(2026, 9, 14, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly EvaluationDate = new DateOnly(2026, 10, 10);

    [Fact]
    public async Task HandleAsync_ShouldBatchFactsExposeRejectionsAndReturnOnlyQualityEligibleParks()
    {
        Park eligiblePark = BuildPark("park-1", "Parc éligible");
        Park rejectedPark = BuildPark("park-2", "Parc incomplet");
        ParkItem attraction = BuildAttraction(eligiblePark.Id);
        ParkOpeningHoursSchedule schedule = BuildSchedule(eligiblePark.Id, isClosed: false);
        ParkOpeningHoursScheduleSummary summary = BuildSummary(eligiblePark.Id);
        Mock<IParkRepository> parks = BuildParkRepository(
            new[] { eligiblePark, rejectedPark },
            totalItems: 250);
        Mock<IParkItemRepository> items = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<IParkOpeningHoursRepository> openingHours =
            new Mock<IParkOpeningHoursRepository>(MockBehavior.Strict);
        items.Setup(repository => repository.GetVisibleOpenAttractionsByParkIdsAsync(
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(
                    new[] { eligiblePark.Id, rejectedPark.Id })),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { attraction });
        openingHours.Setup(repository => repository.GetByParkIdsAsync(
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(
                    new[] { eligiblePark.Id })),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { schedule });
        openingHours.Setup(repository => repository.GetSummariesByParkIdsAsync(
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(
                    new[] { eligiblePark.Id, rejectedPark.Id })),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, ParkOpeningHoursScheduleSummary>
            {
                [eligiblePark.Id] = summary,
            });
        SearchParksByFitQueryHandler handler = BuildHandler(
            parks.Object,
            items.Object,
            openingHours.Object);

        ApplicationResult<ParkFitSearchResult> result = await handler.HandleAsync(BuildQuery());

        Assert.True(result.IsSuccess);
        ParkFitSearchResult value = Assert.IsType<ParkFitSearchResult>(result.Value);
        Assert.Equal(250, value.TotalCandidateCount);
        Assert.Equal(2, value.InspectedCandidateCount);
        Assert.True(value.CandidatePoolTruncated);
        Assert.Equal(1, value.QualityEligibleCandidateCount);
        Assert.Equal(1, value.QualityRejectedCandidateCount);
        Assert.Equal(1, value.QualityIssueCounts[ParkFitDataQualityIssue.NoVisibleAttractions]);
        ParkFitSearchParkResult parkResult = Assert.Single(value.Parks);
        Assert.Equal(eligiblePark.Id, parkResult.Park.Id);
        Assert.Equal(ParkFitScoreState.Available, parkResult.Score.State);
        Assert.Equal(ParkFitDateAvailabilityState.Available, parkResult.Score.DateAvailabilityState);
        Assert.Equal(1, parkResult.EveryoneTogetherAttractionCount);
        ParkFitSearchMemberSummaryResult memberSummary = Assert.Single(parkResult.MemberSummaries);
        Assert.Equal(1, memberSummary.MemberNumber);
        Assert.Equal(1, memberSummary.CompatibleAloneAttractionCount);
        Assert.Equal(0, memberSummary.UnknownAttractionCount);
        Assert.NotEmpty(parkResult.CriticalSources);
        parks.VerifyAll();
        items.VerifyAll();
        openingHours.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenDateIsClosed_ShouldExcludeParkWithoutInventingAvailability()
    {
        Park park = BuildPark("park-1", "Parc fermé ce jour-là");
        ParkItem attraction = BuildAttraction(park.Id);
        Mock<IParkRepository> parks = BuildParkRepository(new[] { park }, totalItems: 1);
        Mock<IParkItemRepository> items = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<IParkOpeningHoursRepository> openingHours =
            new Mock<IParkOpeningHoursRepository>(MockBehavior.Strict);
        items.Setup(repository => repository.GetVisibleOpenAttractionsByParkIdsAsync(
                It.IsAny<IReadOnlyCollection<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { attraction });
        openingHours.Setup(repository => repository.GetByParkIdsAsync(
                It.IsAny<IReadOnlyCollection<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { BuildSchedule(park.Id, isClosed: true) });
        openingHours.Setup(repository => repository.GetSummariesByParkIdsAsync(
                It.IsAny<IReadOnlyCollection<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, ParkOpeningHoursScheduleSummary>
            {
                [park.Id] = BuildSummary(park.Id),
            });
        SearchParksByFitQueryHandler handler = BuildHandler(
            parks.Object,
            items.Object,
            openingHours.Object);

        ApplicationResult<ParkFitSearchResult> result = await handler.HandleAsync(BuildQuery());

        ParkFitSearchParkResult parkResult = Assert.Single(Assert.IsType<ParkFitSearchResult>(
            result.Value).Parks);
        Assert.Equal(ParkFitScoreState.Excluded, parkResult.Score.State);
        Assert.Equal(ParkFitDateAvailabilityState.Unavailable, parkResult.Score.DateAvailabilityState);
    }

    [Fact]
    public async Task HandleAsync_WhenSourcesShareUrlButDiffer_ShouldPreserveDistinctEvidence()
    {
        Park park = BuildPark("park-1", "Parc documenté");
        ParkItem highConfidenceAttraction = BuildAttraction(
            park.Id,
            "item-1",
            AttractionAccessConditionConfidence.High,
            "Taille minimale officielle.");
        ParkItem mediumConfidenceAttraction = BuildAttraction(
            park.Id,
            "item-2",
            AttractionAccessConditionConfidence.Medium,
            "Taille minimale recoupée.");
        Mock<IParkRepository> parks = BuildParkRepository(new[] { park }, totalItems: 1);
        Mock<IParkItemRepository> items = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<IParkOpeningHoursRepository> openingHours =
            new Mock<IParkOpeningHoursRepository>(MockBehavior.Strict);
        items.Setup(repository => repository.GetVisibleOpenAttractionsByParkIdsAsync(
                It.IsAny<IReadOnlyCollection<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { highConfidenceAttraction, mediumConfidenceAttraction });
        openingHours.Setup(repository => repository.GetByParkIdsAsync(
                It.IsAny<IReadOnlyCollection<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { BuildSchedule(park.Id, isClosed: false) });
        openingHours.Setup(repository => repository.GetSummariesByParkIdsAsync(
                It.IsAny<IReadOnlyCollection<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, ParkOpeningHoursScheduleSummary>
            {
                [park.Id] = BuildSummary(park.Id),
            });
        SearchParksByFitQueryHandler handler = BuildHandler(
            parks.Object,
            items.Object,
            openingHours.Object);

        ApplicationResult<ParkFitSearchResult> result = await handler.HandleAsync(BuildQuery());

        ParkFitSearchParkResult parkResult = Assert.Single(Assert.IsType<ParkFitSearchResult>(
            result.Value).Parks);
        Assert.Equal(2, parkResult.CriticalSources.Count);
        Assert.Contains(
            parkResult.CriticalSources,
            source => source.Confidence == AttractionAccessConditionConfidence.High);
        Assert.Contains(
            parkResult.CriticalSources,
            source => source.Confidence == AttractionAccessConditionConfidence.Medium);
    }

    [Fact]
    public async Task HandleAsync_WhenQueryIsInvalid_ShouldNotReadRepositories()
    {
        Mock<IParkRepository> parks = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> items = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<IParkOpeningHoursRepository> openingHours =
            new Mock<IParkOpeningHoursRepository>(MockBehavior.Strict);
        SearchParksByFitQueryHandler handler = BuildHandler(
            parks.Object,
            items.Object,
            openingHours.Object);
        SearchParksByFitQuery invalidQuery = BuildQuery() with
        {
            EvaluationDate = default,
        };

        ApplicationResult<ParkFitSearchResult> result = await handler.HandleAsync(invalidQuery);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);
        Assert.Contains(result.Errors, error => error.Code == "park-fit.search.invalid");
        parks.VerifyNoOtherCalls();
        items.VerifyNoOtherCalls();
        openingHours.VerifyNoOtherCalls();
    }

    private static SearchParksByFitQueryHandler BuildHandler(
        IParkRepository parks,
        IParkItemRepository items,
        IParkOpeningHoursRepository openingHours)
    {
        return new SearchParksByFitQueryHandler(
            parks,
            items,
            openingHours,
            new SearchParksByFitQueryValidator(),
            new ParkFitSearchParkEvaluator(),
            new SearchParksByFitQueryHandlerTestsFixedTimeProvider(EvaluationTimestamp));
    }

    private static Mock<IParkRepository> BuildParkRepository(
        IReadOnlyCollection<Park> candidates,
        long totalItems)
    {
        Mock<IParkRepository> parks = new Mock<IParkRepository>(MockBehavior.Strict);
        parks.Setup(repository => repository.GetPageAsync(
                1,
                ParkFitSearchLimits.MaximumInspectedCandidateCount,
                false,
                true,
                null,
                null,
                "FR",
                true,
                ClosedEntityFilter.OpenOnly,
                It.IsAny<CancellationToken>(),
                ParkAdminSortField.Name,
                false,
                null))
            .ReturnsAsync(new PagedResult<Park>(
                candidates,
                1,
                ParkFitSearchLimits.MaximumInspectedCandidateCount,
                totalItems));
        return parks;
    }

    private static SearchParksByFitQuery BuildQuery()
    {
        return new SearchParksByFitQuery(
            EvaluationDate,
            new[]
            {
                new ParkFitSearchMemberCriteria(
                    "member-1",
                    120,
                    8,
                    8,
                    true,
                    18,
                    70),
            },
            new[] { ParkItemType.FamilyRide },
            true,
            "fr",
            ParkFitUnknownDataPolicy.KeepWithWarning,
            10);
    }

    private static Park BuildPark(string id, string name)
    {
        Park park = new Park
        {
            Id = id,
            Name = name,
            CountryCode = "FR",
            IsVisible = true,
            Type = ParkType.ThemePark,
            Status = ParkStatus.Operating,
            AdminReviewStatus = AdminReviewStatus.Validated,
            Descriptions = new List<LocalizedText>
            {
                new LocalizedText("fr", $"Présentation publique de {name}."),
            },
        };
        park.SetPosition(50, 3);
        return park;
    }

    private static ParkItem BuildAttraction(
        string parkId,
        string id = "item-1",
        AttractionAccessConditionConfidence confidence = AttractionAccessConditionConfidence.High,
        string sourceSummary = "Taille minimale de 100 cm.")
    {
        return new ParkItem
        {
            Id = id,
            ParkId = parkId,
            Name = "Attraction témoin",
            Category = ParkItemCategory.Attraction,
            Type = ParkItemType.FamilyRide,
            IsVisible = true,
            AttractionDetails = new AttractionDetails
            {
                IsIndoor = true,
                AccessConditions = new List<AttractionAccessCondition>
                {
                    new AttractionAccessCondition
                    {
                        Type = AttractionAccessConditionType.MinHeight,
                        Value = 100,
                        Unit = AttractionAccessConditionUnit.Centimeter,
                        SourceKind = AttractionAccessConditionSourceKind.Official,
                        SourceUrl = "https://example.test/access",
                        CollectedAtUtc = EvaluationTimestamp.AddDays(-30),
                        VerifiedAtUtc = EvaluationTimestamp.AddDays(-15),
                        SourceLanguageCode = "fr",
                        SourceSummary = new List<LocalizedText>
                        {
                            new LocalizedText("fr", sourceSummary),
                        },
                        SourceConfidence = confidence,
                        Scope = AttractionAccessConditionScope.Attraction,
                    },
                },
            },
        };
    }

    private static ParkOpeningHoursSchedule BuildSchedule(string parkId, bool isClosed)
    {
        return new ParkOpeningHoursSchedule
        {
            ParkId = parkId,
            TimeZoneId = "UTC",
            DateOverrides = new List<ParkOpeningHoursDateOverride>
            {
                new ParkOpeningHoursDateOverride
                {
                    LocalDate = EvaluationDate,
                    IsClosed = isClosed,
                    TimeRanges = isClosed
                        ? new List<ParkOpeningHoursTimeRange>()
                        : new List<ParkOpeningHoursTimeRange>
                        {
                            new ParkOpeningHoursTimeRange
                            {
                                OpensAt = new TimeOnly(10, 0),
                                ClosesAt = new TimeOnly(18, 0),
                            },
                        },
                },
            },
        };
    }

    private static ParkOpeningHoursScheduleSummary BuildSummary(string parkId)
    {
        DateOnly today = DateOnly.FromDateTime(EvaluationTimestamp);
        return new ParkOpeningHoursScheduleSummary
        {
            ParkId = parkId,
            TimeZoneId = "UTC",
            HasScheduleData = true,
            LastDate = today.AddDays(60),
            CoverageSegments = new[]
            {
                new ParkOpeningHoursCoverageSegmentSummary
                {
                    StartDate = today,
                    EndDate = today.AddDays(60),
                },
            },
        };
    }
}
