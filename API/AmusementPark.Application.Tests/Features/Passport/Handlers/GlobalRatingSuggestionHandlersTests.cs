using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Passport.Commands;
using AmusementPark.Application.Features.Passport.Handlers;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Application.Features.Passport.Queries;
using AmusementPark.Application.Features.Passport.Results;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Passport.Handlers;

public sealed class GlobalRatingSuggestionHandlersTests
{
    private static readonly DateTime NowUtc =
        new DateTime(2026, 9, 4, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Query_WithEligibleObservations_ReturnsExplanationWithoutMutatingRating()
    {
        Mock<IGlobalRatingSuggestionSourceReader> sources =
            new Mock<IGlobalRatingSuggestionSourceReader>(MockBehavior.Strict);
        sources.Setup(value => value.ReadAsync("owner-1", CancellationToken.None))
            .ReturnsAsync(new[] { CreateSource() });
        Mock<IGlobalRatingSuggestionStateRepository> states = CreateEnabledStates();
        states.Setup(value => value.GetStatesAsync(
                "owner-1",
                It.IsAny<IReadOnlyCollection<GlobalRatingSuggestionTargetKey>>(),
                CancellationToken.None))
            .ReturnsAsync(Array.Empty<GlobalRatingSuggestionTargetState>());
        Mock<IParkRepository> parks = new Mock<IParkRepository>(MockBehavior.Strict);
        parks.Setup(value => value.GetByIdAsync("park-1", false, CancellationToken.None))
            .ReturnsAsync(new Park { Id = "park-1", Name = "Parc test", Status = ParkStatus.Operating });
        Mock<IParkItemRepository> items = new Mock<IParkItemRepository>(MockBehavior.Strict);
        GetGlobalRatingSuggestionsQueryHandler handler = new GetGlobalRatingSuggestionsQueryHandler(
            sources.Object,
            states.Object,
            new FeatureGate(true),
            parks.Object,
            items.Object,
            new TestClock(NowUtc),
            new GlobalRatingSuggestionPolicy());

        ApplicationResult<GlobalRatingSuggestionsResult> result = await handler.HandleAsync(
            new GetGlobalRatingSuggestionsQuery(" owner-1 "));

        Assert.True(result.IsSuccess);
        GlobalRatingSuggestionResult suggestion = Assert.Single(result.Value!.Suggestions);
        Assert.Equal("Parc test", suggestion.TargetName);
        Assert.Equal(GlobalRatingSuggestionReason.RecentExperiencesLower, suggestion.Reason);
        Assert.Equal(4.5d, suggestion.CurrentGlobalRating);
        Assert.Equal(3.25d, suggestion.RecentAverage);
        sources.VerifyAll();
        states.VerifyAll();
        parks.VerifyAll();
        items.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Query_UsesTheCurrentItemCategoryForNavigation()
    {
        GlobalRatingSuggestionSource source = new GlobalRatingSuggestionSource(
            RatingTargetType.ParkItem,
            "item-1",
            "park-1",
            ParkItemCategory.Attraction,
            ParkItemType.RollerCoaster,
            RatingValue.FromDouble(4.5d),
            NowUtc.AddDays(-10),
            new[]
            {
                new GlobalRatingSuggestionObservation(
                    RatingValue.FromDouble(3d),
                    NowUtc.AddDays(-1)),
                new GlobalRatingSuggestionObservation(
                    RatingValue.FromDouble(3.5d),
                    NowUtc.AddDays(-2)),
            });
        Mock<IGlobalRatingSuggestionSourceReader> sources =
            new Mock<IGlobalRatingSuggestionSourceReader>(MockBehavior.Strict);
        sources.Setup(value => value.ReadAsync("owner-1", CancellationToken.None))
            .ReturnsAsync(new[] { source });
        Mock<IGlobalRatingSuggestionStateRepository> states = CreateEnabledStates();
        states.Setup(value => value.GetStatesAsync(
                "owner-1",
                It.IsAny<IReadOnlyCollection<GlobalRatingSuggestionTargetKey>>(),
                CancellationToken.None))
            .ReturnsAsync(Array.Empty<GlobalRatingSuggestionTargetState>());
        Mock<IParkRepository> parks = new Mock<IParkRepository>(MockBehavior.Strict);
        parks.Setup(value => value.GetByIdAsync("park-1", false, CancellationToken.None))
            .ReturnsAsync(new Park
            {
                Id = "park-1",
                Name = "Parc test",
                Status = ParkStatus.Operating,
            });
        Mock<IParkItemRepository> items = new Mock<IParkItemRepository>(MockBehavior.Strict);
        items.Setup(value => value.GetByIdAsync("item-1", false, CancellationToken.None))
            .ReturnsAsync(new ParkItem
            {
                Id = "item-1",
                ParkId = "park-1",
                Name = "Restaurant recatégorisé",
                Category = ParkItemCategory.Restaurant,
                Type = ParkItemType.Restaurant,
            });
        GetGlobalRatingSuggestionsQueryHandler handler = new GetGlobalRatingSuggestionsQueryHandler(
            sources.Object,
            states.Object,
            new FeatureGate(true),
            parks.Object,
            items.Object,
            new TestClock(NowUtc),
            new GlobalRatingSuggestionPolicy());

        ApplicationResult<GlobalRatingSuggestionsResult> result = await handler.HandleAsync(
            new GetGlobalRatingSuggestionsQuery("owner-1"));

        Assert.True(result.IsSuccess);
        Assert.Equal(
            ParkItemCategory.Restaurant,
            Assert.Single(result.Value!.Suggestions).ParkItemCategory);
        sources.VerifyAll();
        states.VerifyAll();
        parks.VerifyAll();
        items.VerifyAll();
    }

    [Fact]
    public async Task Query_WhenKillSwitchIsOff_DoesNotReadPrivateObservations()
    {
        Mock<IGlobalRatingSuggestionSourceReader> sources =
            new Mock<IGlobalRatingSuggestionSourceReader>(MockBehavior.Strict);
        Mock<IGlobalRatingSuggestionStateRepository> states =
            new Mock<IGlobalRatingSuggestionStateRepository>(MockBehavior.Strict);
        GetGlobalRatingSuggestionsQueryHandler handler = new GetGlobalRatingSuggestionsQueryHandler(
            sources.Object,
            states.Object,
            new FeatureGate(false),
            new Mock<IParkRepository>(MockBehavior.Strict).Object,
            new Mock<IParkItemRepository>(MockBehavior.Strict).Object,
            new TestClock(NowUtc),
            new GlobalRatingSuggestionPolicy());

        ApplicationResult<GlobalRatingSuggestionsResult> result = await handler.HandleAsync(
            new GetGlobalRatingSuggestionsQuery("owner-1"));

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.IsAvailable);
        Assert.Empty(result.Value.Suggestions);
        sources.VerifyNoOtherCalls();
        states.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Interaction_Presented_IsRejectedInFavorOfTheBoundedBatch()
    {
        Mock<IGlobalRatingSuggestionStateRepository> states =
            new Mock<IGlobalRatingSuggestionStateRepository>(MockBehavior.Strict);
        Mock<IRatingRepository> ratings = new Mock<IRatingRepository>(MockBehavior.Strict);
        RecordGlobalRatingSuggestionInteractionCommandHandler handler =
            new RecordGlobalRatingSuggestionInteractionCommandHandler(
                states.Object,
                new FeatureGate(true),
                ratings.Object,
                new TestClock(NowUtc),
                new GlobalRatingSuggestionPolicy());

        ApplicationResult<GlobalRatingSuggestionPreferenceResult> result =
            await handler.HandleAsync(new RecordGlobalRatingSuggestionInteractionCommand(
                "owner-1",
                RatingTargetType.Park,
                "park-1",
                GlobalRatingSuggestionInteractionType.Presented,
                NowUtc));

        Assert.False(result.IsSuccess);
        Assert.Equal(
            "passport.rating-suggestion-interaction-invalid",
            Assert.Single(result.Errors).Code);
        states.VerifyNoOtherCalls();
        ratings.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task PresentationBatch_RevalidatesAllTargetsWithOnePrivateSourceRead()
    {
        GlobalRatingSuggestionSource secondSource = CreateSource() with
        {
            TargetId = "park-2",
            ParkId = "park-2",
        };
        Mock<IGlobalRatingSuggestionSourceReader> sources =
            new Mock<IGlobalRatingSuggestionSourceReader>(MockBehavior.Strict);
        sources.Setup(value => value.ReadAsync("owner-1", CancellationToken.None))
            .ReturnsAsync(new[] { CreateSource(), secondSource });
        Mock<IGlobalRatingSuggestionStateRepository> states = CreateEnabledStates();
        states.Setup(value => value.GetStatesAsync(
                "owner-1",
                It.Is<IReadOnlyCollection<GlobalRatingSuggestionTargetKey>>(
                    targets => targets.Count == 2),
                CancellationToken.None))
            .ReturnsAsync(Array.Empty<GlobalRatingSuggestionTargetState>());
        states.Setup(value => value.TryRecordInteractionAsync(
                "owner-1",
                It.IsAny<RatingTargetType>(),
                It.IsAny<string>(),
                null,
                GlobalRatingSuggestionInteractionType.Presented,
                NowUtc,
                CancellationToken.None))
            .ReturnsAsync(true);
        Mock<IParkRepository> parks = new Mock<IParkRepository>(MockBehavior.Strict);
        parks.Setup(value => value.GetByIdAsync(
                It.IsAny<string>(),
                false,
                CancellationToken.None))
            .ReturnsAsync((string id, bool _, CancellationToken _) => new Park
            {
                Id = id,
                Name = id,
                Status = ParkStatus.Operating,
            });
        PresentGlobalRatingSuggestionsCommandHandler handler =
            new PresentGlobalRatingSuggestionsCommandHandler(
                sources.Object,
                states.Object,
                new FeatureGate(true),
                parks.Object,
                new Mock<IParkItemRepository>(MockBehavior.Strict).Object,
                new TestClock(NowUtc),
                new GlobalRatingSuggestionPolicy());

        ApplicationResult<GlobalRatingSuggestionPresentationResult> result =
            await handler.HandleAsync(new PresentGlobalRatingSuggestionsCommand(
                "owner-1",
                new[]
                {
                    new GlobalRatingSuggestionTargetKey(RatingTargetType.Park, "park-1"),
                    new GlobalRatingSuggestionTargetKey(RatingTargetType.Park, "park-2"),
                }));

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.PresentedTargets.Count);
        sources.Verify(
            value => value.ReadAsync("owner-1", CancellationToken.None),
            Times.Once);
        states.VerifyAll();
        parks.VerifyAll();
    }

    [Fact]
    public async Task PresentationBatch_IneligibleTarget_IsNotAcknowledgedOrRecorded()
    {
        Mock<IGlobalRatingSuggestionStateRepository> states =
            new Mock<IGlobalRatingSuggestionStateRepository>(MockBehavior.Strict);
        states.Setup(value => value.IsEnabledAsync("owner-1", CancellationToken.None))
            .ReturnsAsync(true);
        states.Setup(value => value.GetStatesAsync(
                "owner-1",
                It.IsAny<IReadOnlyCollection<GlobalRatingSuggestionTargetKey>>(),
                CancellationToken.None))
            .ReturnsAsync(Array.Empty<GlobalRatingSuggestionTargetState>());
        Mock<IGlobalRatingSuggestionSourceReader> sources =
            new Mock<IGlobalRatingSuggestionSourceReader>(MockBehavior.Strict);
        sources.Setup(value => value.ReadAsync("owner-1", CancellationToken.None))
            .ReturnsAsync(Array.Empty<GlobalRatingSuggestionSource>());
        PresentGlobalRatingSuggestionsCommandHandler handler =
            new PresentGlobalRatingSuggestionsCommandHandler(
                sources.Object,
                states.Object,
                new FeatureGate(true),
                new Mock<IParkRepository>(MockBehavior.Strict).Object,
                new Mock<IParkItemRepository>(MockBehavior.Strict).Object,
                new TestClock(NowUtc),
                new GlobalRatingSuggestionPolicy());

        ApplicationResult<GlobalRatingSuggestionPresentationResult> result =
            await handler.HandleAsync(new PresentGlobalRatingSuggestionsCommand(
                "owner-1",
                new[]
                {
                    new GlobalRatingSuggestionTargetKey(RatingTargetType.Park, "park-1"),
                }));

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!.PresentedTargets);
        states.VerifyAll();
        sources.VerifyAll();
    }

    [Fact]
    public async Task Interaction_RecordsOnlyExplicitAcceptanceForAnOwnedRating()
    {
        DateTime presentedAtUtc = NowUtc.AddHours(-1);
        Mock<IGlobalRatingSuggestionStateRepository> states =
            new Mock<IGlobalRatingSuggestionStateRepository>(MockBehavior.Strict);
        states.Setup(value => value.IsEnabledAsync("owner-1", CancellationToken.None))
            .ReturnsAsync(true);
        states.Setup(value => value.GetStatesAsync(
                "owner-1",
                It.IsAny<IReadOnlyCollection<GlobalRatingSuggestionTargetKey>>(),
                CancellationToken.None))
            .ReturnsAsync(new[]
            {
                new GlobalRatingSuggestionTargetState(
                    RatingTargetType.Park,
                    "park-1",
                    presentedAtUtc,
                    null,
                    null,
                    true),
            });
        states.Setup(value => value.TryRecordInteractionAsync(
                "owner-1",
                RatingTargetType.Park,
                "park-1",
                presentedAtUtc,
                GlobalRatingSuggestionInteractionType.Accepted,
                NowUtc,
                CancellationToken.None))
            .ReturnsAsync(true);
        Mock<IRatingRepository> ratings = new Mock<IRatingRepository>(MockBehavior.Strict);
        ratings.Setup(value => value.GetUserRatingAsync(
                "owner-1",
                RatingTargetType.Park,
                "park-1",
                CancellationToken.None))
            .ReturnsAsync(new UserRating
            {
                Id = "rating-1",
                UserId = "owner-1",
                TargetType = RatingTargetType.Park,
                TargetId = "park-1",
                ParkId = "park-1",
                Value = 4.5d,
            });
        RecordGlobalRatingSuggestionInteractionCommandHandler handler =
            new RecordGlobalRatingSuggestionInteractionCommandHandler(
                states.Object,
                new FeatureGate(true),
                ratings.Object,
                new TestClock(NowUtc),
                new GlobalRatingSuggestionPolicy());

        ApplicationResult<GlobalRatingSuggestionPreferenceResult> result =
            await handler.HandleAsync(new RecordGlobalRatingSuggestionInteractionCommand(
                "owner-1",
                RatingTargetType.Park,
                "park-1",
                GlobalRatingSuggestionInteractionType.Accepted,
                presentedAtUtc));

        Assert.True(result.IsSuccess);
        ratings.VerifyAll();
        states.VerifyAll();
    }

    [Fact]
    public async Task Interaction_WithMissingRating_DoesNotCreateSuggestionState()
    {
        Mock<IGlobalRatingSuggestionStateRepository> states =
            new Mock<IGlobalRatingSuggestionStateRepository>(MockBehavior.Strict);
        states.Setup(value => value.IsEnabledAsync("owner-1", CancellationToken.None))
            .ReturnsAsync(true);
        Mock<IRatingRepository> ratings = new Mock<IRatingRepository>(MockBehavior.Strict);
        ratings.Setup(value => value.GetUserRatingAsync(
                "owner-1",
                RatingTargetType.Park,
                "park-1",
                CancellationToken.None))
            .ReturnsAsync((UserRating?)null);
        RecordGlobalRatingSuggestionInteractionCommandHandler handler =
            new RecordGlobalRatingSuggestionInteractionCommandHandler(
                states.Object,
                new FeatureGate(true),
                ratings.Object,
                new TestClock(NowUtc),
                new GlobalRatingSuggestionPolicy());

        ApplicationResult<GlobalRatingSuggestionPreferenceResult> result =
            await handler.HandleAsync(new RecordGlobalRatingSuggestionInteractionCommand(
                "owner-1",
                RatingTargetType.Park,
                "park-1",
                GlobalRatingSuggestionInteractionType.Dismissed,
                NowUtc));

        Assert.False(result.IsSuccess);
        Assert.Equal(
            "passport.rating-suggestion-target-not-found",
            Assert.Single(result.Errors).Code);
        states.VerifyAll();
        ratings.VerifyAll();
    }

    [Fact]
    public async Task Interaction_WhenKillSwitchIsOff_DoesNotReadOrWritePrivateState()
    {
        Mock<IGlobalRatingSuggestionStateRepository> states =
            new Mock<IGlobalRatingSuggestionStateRepository>(MockBehavior.Strict);
        Mock<IRatingRepository> ratings = new Mock<IRatingRepository>(MockBehavior.Strict);
        RecordGlobalRatingSuggestionInteractionCommandHandler handler =
            new RecordGlobalRatingSuggestionInteractionCommandHandler(
                states.Object,
                new FeatureGate(false),
                ratings.Object,
                new TestClock(NowUtc),
                new GlobalRatingSuggestionPolicy());

        ApplicationResult<GlobalRatingSuggestionPreferenceResult> result =
            await handler.HandleAsync(new RecordGlobalRatingSuggestionInteractionCommand(
                "owner-1",
                RatingTargetType.Park,
                "park-1",
                GlobalRatingSuggestionInteractionType.Accepted,
                NowUtc));

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.IsAvailable);
        states.VerifyNoOtherCalls();
        ratings.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Interaction_WithoutCurrentPresentation_IsRejectedWithoutAnalytics()
    {
        Mock<IGlobalRatingSuggestionStateRepository> states =
            new Mock<IGlobalRatingSuggestionStateRepository>(MockBehavior.Strict);
        states.Setup(value => value.IsEnabledAsync("owner-1", CancellationToken.None))
            .ReturnsAsync(true);
        states.Setup(value => value.GetStatesAsync(
                "owner-1",
                It.IsAny<IReadOnlyCollection<GlobalRatingSuggestionTargetKey>>(),
                CancellationToken.None))
            .ReturnsAsync(Array.Empty<GlobalRatingSuggestionTargetState>());
        Mock<IRatingRepository> ratings = new Mock<IRatingRepository>(MockBehavior.Strict);
        ratings.Setup(value => value.GetUserRatingAsync(
                "owner-1",
                RatingTargetType.Park,
                "park-1",
                CancellationToken.None))
            .ReturnsAsync(new UserRating
            {
                Id = "rating-1",
                UserId = "owner-1",
                TargetType = RatingTargetType.Park,
                TargetId = "park-1",
                ParkId = "park-1",
                Value = 4.5d,
            });
        RecordGlobalRatingSuggestionInteractionCommandHandler handler =
            new RecordGlobalRatingSuggestionInteractionCommandHandler(
                states.Object,
                new FeatureGate(true),
                ratings.Object,
                new TestClock(NowUtc),
                new GlobalRatingSuggestionPolicy());

        ApplicationResult<GlobalRatingSuggestionPreferenceResult> result =
            await handler.HandleAsync(new RecordGlobalRatingSuggestionInteractionCommand(
                "owner-1",
                RatingTargetType.Park,
                "park-1",
                GlobalRatingSuggestionInteractionType.Accepted,
                NowUtc));

        Assert.False(result.IsSuccess);
        Assert.Equal(
            "passport.rating-suggestion-interaction-invalid",
            Assert.Single(result.Errors).Code);
        states.VerifyAll();
        ratings.VerifyAll();
    }

    [Fact]
    public async Task Interaction_FromAnOlderPresentation_CannotResolveTheCurrentPresentation()
    {
        DateTime oldPresentationAtUtc = NowUtc.AddDays(-31);
        DateTime currentPresentationAtUtc = NowUtc.AddHours(-1);
        Mock<IGlobalRatingSuggestionStateRepository> states =
            new Mock<IGlobalRatingSuggestionStateRepository>(MockBehavior.Strict);
        states.Setup(value => value.IsEnabledAsync("owner-1", CancellationToken.None))
            .ReturnsAsync(true);
        states.Setup(value => value.GetStatesAsync(
                "owner-1",
                It.IsAny<IReadOnlyCollection<GlobalRatingSuggestionTargetKey>>(),
                CancellationToken.None))
            .ReturnsAsync(new[]
            {
                new GlobalRatingSuggestionTargetState(
                    RatingTargetType.Park,
                    "park-1",
                    currentPresentationAtUtc,
                    null,
                    null,
                    true),
            });
        Mock<IRatingRepository> ratings = new Mock<IRatingRepository>(MockBehavior.Strict);
        ratings.Setup(value => value.GetUserRatingAsync(
                "owner-1",
                RatingTargetType.Park,
                "park-1",
                CancellationToken.None))
            .ReturnsAsync(CreateOwnedRating());
        RecordGlobalRatingSuggestionInteractionCommandHandler handler =
            new RecordGlobalRatingSuggestionInteractionCommandHandler(
                states.Object,
                new FeatureGate(true),
                ratings.Object,
                new TestClock(NowUtc),
                new GlobalRatingSuggestionPolicy());

        ApplicationResult<GlobalRatingSuggestionPreferenceResult> result =
            await handler.HandleAsync(new RecordGlobalRatingSuggestionInteractionCommand(
                "owner-1",
                RatingTargetType.Park,
                "park-1",
                GlobalRatingSuggestionInteractionType.Accepted,
                oldPresentationAtUtc));

        Assert.False(result.IsSuccess);
        Assert.Equal(
            "passport.rating-suggestion-interaction-invalid",
            Assert.Single(result.Errors).Code);
        states.VerifyAll();
        states.Verify(value => value.TryRecordInteractionAsync(
                It.IsAny<string>(),
                It.IsAny<RatingTargetType>(),
                It.IsAny<string>(),
                It.IsAny<DateTime?>(),
                It.IsAny<GlobalRatingSuggestionInteractionType>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        ratings.VerifyAll();
    }

    [Fact]
    public async Task Interaction_ReplayedAcceptance_IsIdempotentWithoutDuplicateAnalytics()
    {
        DateTime presentedAtUtc = NowUtc.AddHours(-1);
        Mock<IGlobalRatingSuggestionStateRepository> states =
            new Mock<IGlobalRatingSuggestionStateRepository>(MockBehavior.Strict);
        states.Setup(value => value.IsEnabledAsync("owner-1", CancellationToken.None))
            .ReturnsAsync(true);
        states.Setup(value => value.GetStatesAsync(
                "owner-1",
                It.IsAny<IReadOnlyCollection<GlobalRatingSuggestionTargetKey>>(),
                CancellationToken.None))
            .ReturnsAsync(new[]
            {
                new GlobalRatingSuggestionTargetState(
                    RatingTargetType.Park,
                    "park-1",
                    presentedAtUtc,
                    NowUtc.AddMinutes(-30),
                    null,
                    false),
            });
        Mock<IRatingRepository> ratings = new Mock<IRatingRepository>(MockBehavior.Strict);
        ratings.Setup(value => value.GetUserRatingAsync(
                "owner-1",
                RatingTargetType.Park,
                "park-1",
                CancellationToken.None))
            .ReturnsAsync(CreateOwnedRating());
        RecordGlobalRatingSuggestionInteractionCommandHandler handler =
            new RecordGlobalRatingSuggestionInteractionCommandHandler(
                states.Object,
                new FeatureGate(true),
                ratings.Object,
                new TestClock(NowUtc),
                new GlobalRatingSuggestionPolicy());

        ApplicationResult<GlobalRatingSuggestionPreferenceResult> result =
            await handler.HandleAsync(new RecordGlobalRatingSuggestionInteractionCommand(
                "owner-1",
                RatingTargetType.Park,
                "park-1",
                GlobalRatingSuggestionInteractionType.Accepted,
                presentedAtUtc));

        Assert.True(result.IsSuccess);
        states.VerifyAll();
        ratings.VerifyAll();
    }

    private static Mock<IGlobalRatingSuggestionStateRepository> CreateEnabledStates()
    {
        Mock<IGlobalRatingSuggestionStateRepository> states =
            new Mock<IGlobalRatingSuggestionStateRepository>(MockBehavior.Strict);
        states.Setup(value => value.IsEnabledAsync("owner-1", CancellationToken.None))
            .ReturnsAsync(true);
        return states;
    }

    private static GlobalRatingSuggestionSource CreateSource()
    {
        return new GlobalRatingSuggestionSource(
            RatingTargetType.Park,
            "park-1",
            "park-1",
            null,
            null,
            RatingValue.FromDouble(4.5d),
            NowUtc.AddDays(-10),
            new[]
            {
                new GlobalRatingSuggestionObservation(
                    RatingValue.FromDouble(3d),
                    NowUtc.AddDays(-1)),
                new GlobalRatingSuggestionObservation(
                    RatingValue.FromDouble(3.5d),
                    NowUtc.AddDays(-2)),
            });
    }

    private static UserRating CreateOwnedRating()
    {
        return new UserRating
        {
            Id = "rating-1",
            UserId = "owner-1",
            TargetType = RatingTargetType.Park,
            TargetId = "park-1",
            ParkId = "park-1",
            Value = 4.5d,
        };
    }

    private sealed class FeatureGate : IGlobalRatingSuggestionFeatureGate
    {
        public FeatureGate(bool isEnabled)
        {
            this.IsEnabled = isEnabled;
        }

        public bool IsEnabled { get; }
    }

    private sealed class TestClock : IPassportClock
    {
        public TestClock(DateTime utcNow)
        {
            this.UtcNow = utcNow;
        }

        public DateTime UtcNow { get; }
    }
}
