using AmusementPark.Application.Features.Ratings.Models;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Ratings.Services;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Application.Features.Sharing.Services;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;
using Moq;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Ratings.Services;

public sealed class RatingRankingSourceRevisionGuardTests
{
    [Fact]
    public async Task PrepareMutationAsync_ShouldAlsoFenceTheOwnersPersonalRankingShare()
    {
        Mock<IRatingRankingSourceRevisionRepository> revisions =
            new Mock<IRatingRankingSourceRevisionRepository>(MockBehavior.Strict);
        revisions.Setup(value => value.BeginMutationAsync(
                CanonicalRankingScopes.GlobalParks.Key,
                It.IsAny<RatingRankingMutationRecoveryTarget>(),
                CancellationToken.None))
            .ReturnsAsync(CreateLease(CanonicalRankingScopes.GlobalParks.Key));
        ShareSourceMutationLease shareLease = new ShareSourceMutationLease(
            "personal-ranking:user-1",
            8.ToString("x32"));
        string selectedRatingScope = PersonalRankingShareSourceScope.CreateRating(
            "user-1",
            PassportProfileRatingSelectionKey.Create(RatingTargetType.Park, "park-1"));
        ShareSourceMutationLease selectedRatingLease = new ShareSourceMutationLease(
            selectedRatingScope,
            10.ToString("x32"));
        Mock<IShareSourceRevisionRepository> shareRevisions =
            new Mock<IShareSourceRevisionRepository>(MockBehavior.Strict);
        shareRevisions.Setup(value => value.BeginMutationAsync(
                "personal-ranking:user-1",
                CancellationToken.None))
            .ReturnsAsync(shareLease);
        shareRevisions.Setup(value => value.BeginMutationAsync(
                selectedRatingScope,
                CancellationToken.None))
            .ReturnsAsync(selectedRatingLease);
        RatingRankingSourceRevisionGuard guard = CreateGuard(
            revisions.Object,
            shareSourceRevisions: shareRevisions.Object);

        RatingRankingMutationPreparation preparation = await guard.PrepareMutationAsync(
            CreateRecoveryTarget(RatingTargetType.Park, "park-1"),
            null,
            null,
            CancellationToken.None);

        Assert.Same(shareLease, preparation.PersonalRankingShareMutationLease);
        Assert.Same(
            selectedRatingLease,
            Assert.Single(preparation.PersonalRatingShareMutationLeases));
        revisions.VerifyAll();
        shareRevisions.VerifyAll();
    }

    [Fact]
    public async Task CompleteMutationAsync_WhenRatingChanged_ShouldAdvancePersonalShareRevision()
    {
        Mock<IRatingRankingSourceRevisionRepository> revisions =
            new Mock<IRatingRankingSourceRevisionRepository>(MockBehavior.Strict);
        ShareSourceMutationLease shareLease = new ShareSourceMutationLease(
            "personal-ranking:user-1",
            8.ToString("x32"));
        Mock<IShareSourceRevisionRepository> shareRevisions =
            new Mock<IShareSourceRevisionRepository>(MockBehavior.Strict);
        shareRevisions.Setup(value => value.CompleteMutationAsync(
                shareLease,
                true,
                CancellationToken.None))
            .ReturnsAsync(new ShareSourceRevision(5, 0, DateTime.UtcNow));
        RatingRankingSourceRevisionGuard guard = CreateGuard(
            revisions.Object,
            shareSourceRevisions: shareRevisions.Object);

        await guard.CompleteMutationAsync(
            new RatingRankingMutationPreparation(
                Array.Empty<RatingRankingMutationLease>(),
                shareLease),
            sourceChanged: true,
            CancellationToken.None);

        revisions.VerifyNoOtherCalls();
        shareRevisions.VerifyAll();
    }

    [Fact]
    public async Task CompleteMutationAsync_WhenCatalogChanged_ShouldAdvanceCatalogShareRevision()
    {
        Mock<IRatingRankingSourceRevisionRepository> revisions =
            new Mock<IRatingRankingSourceRevisionRepository>(MockBehavior.Strict);
        ShareSourceMutationLease catalogLease = new ShareSourceMutationLease(
            "personal-ranking:public-catalog",
            12.ToString("x32"));
        Mock<IShareSourceRevisionRepository> shareRevisions =
            new Mock<IShareSourceRevisionRepository>(MockBehavior.Strict);
        shareRevisions.Setup(value => value.CompleteMutationAsync(
                catalogLease,
                true,
                CancellationToken.None))
            .ReturnsAsync(new ShareSourceRevision(8, 0, DateTime.UtcNow));
        RatingRankingSourceRevisionGuard guard = CreateGuard(
            revisions.Object,
            shareSourceRevisions: shareRevisions.Object);

        await guard.CompleteMutationAsync(
            new RatingRankingMutationPreparation(
                Array.Empty<RatingRankingMutationLease>(),
                null,
                catalogLease),
            sourceChanged: true,
            CancellationToken.None);

        revisions.VerifyNoOtherCalls();
        shareRevisions.VerifyAll();
    }

    private static readonly DateTime NowUtc = new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task PrepareMutationAsync_WhenParkItemCategoryChanged_ShouldInvalidateBothCategoriesAndParkScope()
    {
        List<RankingScopeKey> incrementedScopes = new List<RankingScopeKey>();
        Mock<IRatingRankingSourceRevisionRepository> revisions =
            new Mock<IRatingRankingSourceRevisionRepository>(MockBehavior.Strict);
        revisions
            .Setup(repository => repository.BeginMutationAsync(
                It.IsAny<RankingScopeKey>(),
                CancellationToken.None))
            .Callback((RankingScopeKey scopeKey, CancellationToken _) => incrementedScopes.Add(scopeKey))
            .Returns((RankingScopeKey scopeKey, CancellationToken _) =>
                Task.FromResult(CreateLease(scopeKey)));
        revisions
            .Setup(repository => repository.BeginMutationAsync(
                It.IsAny<RankingScopeKey>(),
                It.IsAny<RatingRankingMutationRecoveryTarget>(),
                CancellationToken.None))
            .Callback((
                RankingScopeKey scopeKey,
                RatingRankingMutationRecoveryTarget _,
                CancellationToken _) => incrementedScopes.Add(scopeKey))
            .Returns((
                RankingScopeKey scopeKey,
                RatingRankingMutationRecoveryTarget _,
                CancellationToken _) => Task.FromResult(CreateLease(scopeKey)));
        RatingRankingSourceRevisionGuard guard = CreateGuard(revisions.Object);

        RatingRankingMutationPreparation preparation = await guard.PrepareMutationAsync(
            CreateRecoveryTarget(RatingTargetType.ParkItem, "item-1"),
            ParkItemCategory.Attraction,
            ParkItemCategory.Show,
            CancellationToken.None);

        Assert.Equal(
            new[] { "park-items:category:attraction", "park-items:category:show", "parks:global" },
            incrementedScopes.Select(static scopeKey => scopeKey.Value));
        Assert.Equal(3, preparation.MutationLeases.Count);
        revisions.VerifyAll();
    }

    [Fact]
    public async Task PrepareMutationAsync_ShouldPersistRecoveryTargetOnGlobalFenceOnly()
    {
        RatingRankingMutationRecoveryTarget? capturedRecoveryTarget = null;
        RankingScopeKey categoryScopeKey = RankingScopeKey.Parse("park-items:category:attraction");
        RankingScopeKey globalScopeKey = RankingScopeKey.Parse("parks:global");
        Mock<IRatingRankingSourceRevisionRepository> revisions =
            new Mock<IRatingRankingSourceRevisionRepository>(MockBehavior.Strict);
        revisions
            .Setup(repository => repository.BeginMutationAsync(
                categoryScopeKey,
                CancellationToken.None))
            .ReturnsAsync(CreateLease(categoryScopeKey));
        revisions
            .Setup(repository => repository.BeginMutationAsync(
                globalScopeKey,
                It.IsAny<RatingRankingMutationRecoveryTarget>(),
                CancellationToken.None))
            .Callback((
                RankingScopeKey _,
                RatingRankingMutationRecoveryTarget recoveryTarget,
                CancellationToken _) => capturedRecoveryTarget = recoveryTarget)
            .ReturnsAsync(CreateLease(globalScopeKey));
        RatingRankingSourceRevisionGuard guard = CreateGuard(revisions.Object);

        RatingRankingMutationPreparation preparation = await guard.PrepareMutationAsync(
            CreateRecoveryTarget(RatingTargetType.ParkItem, " item-1 "),
            ParkItemCategory.Attraction,
            null,
            CancellationToken.None);

        Assert.Equal(2, preparation.MutationLeases.Count);
        Assert.NotNull(capturedRecoveryTarget);
        Assert.Equal(RatingTargetType.ParkItem, capturedRecoveryTarget.TargetType);
        Assert.Equal("item-1", capturedRecoveryTarget.TargetId);
        Assert.Equal("user-1", capturedRecoveryTarget.UserId);
        Assert.Equal(9.ToString("x32"), capturedRecoveryTarget.MutationToken);
        revisions.VerifyAll();
    }

    [Fact]
    public async Task PrepareMutationAsync_WhenARevisionCannotBePersisted_ShouldAbortThePreparation()
    {
        RankingScopeKey categoryScopeKey = RankingScopeKey.Parse("park-items:category:attraction");
        Mock<IRatingRankingSourceRevisionRepository> revisions =
            new Mock<IRatingRankingSourceRevisionRepository>(MockBehavior.Strict);
        revisions
            .Setup(repository => repository.BeginMutationAsync(categoryScopeKey, CancellationToken.None))
            .ThrowsAsync(new InvalidOperationException("Mongo unavailable"));
        RatingRankingSourceRevisionGuard guard = CreateGuard(revisions.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() => guard.PrepareMutationAsync(
            CreateRecoveryTarget(RatingTargetType.ParkItem, "item-1"),
            ParkItemCategory.Attraction,
            null,
            CancellationToken.None));

        revisions.VerifyAll();
    }

    [Fact]
    public async Task PrepareMutationAsync_WhenParkChanges_ShouldOnlyInvalidateTheGlobalParkScope()
    {
        RankingScopeKey globalScopeKey = RankingScopeKey.Parse("parks:global");
        Mock<IRatingRankingSourceRevisionRepository> revisions =
            new Mock<IRatingRankingSourceRevisionRepository>(MockBehavior.Strict);
        revisions
            .Setup(repository => repository.BeginMutationAsync(
                globalScopeKey,
                It.Is<RatingRankingMutationRecoveryTarget>(target =>
                    target.TargetType == RatingTargetType.Park
                    && target.TargetId == "park-1"),
                CancellationToken.None))
            .ReturnsAsync(CreateLease(globalScopeKey));
        RatingRankingSourceRevisionGuard guard = CreateGuard(revisions.Object);

        RatingRankingMutationPreparation preparation = await guard.PrepareMutationAsync(
            CreateRecoveryTarget(RatingTargetType.Park, "park-1"),
            null,
            null,
            CancellationToken.None);

        Assert.Single(preparation.MutationLeases);
        revisions.VerifyAll();
    }

    [Fact]
    public async Task CompleteMutationAsync_WhenOneScheduleFails_ShouldContinueWithRemainingRevisions()
    {
        RatingRankingSourceRevision first = new RatingRankingSourceRevision(
            RankingScopeKey.Parse("park-items:category:attraction"),
            4,
            NowUtc);
        RatingRankingSourceRevision second = new RatingRankingSourceRevision(
            RankingScopeKey.Parse("parks:global"),
            7,
            NowUtc);
        RatingRankingMutationLease firstLease = CreateLease(first.ScopeKey, 1);
        RatingRankingMutationLease secondLease = CreateLease(second.ScopeKey, 2);
        RatingMethodologyVersion itemMethodologyVersion =
            CanonicalRankingScopes.PublicItemCategories.Single(
                static scope => scope.Key.Value == "park-items:category:attraction").MethodologyVersion;
        Mock<IRatingRankingRebuildScheduler> scheduler =
            new Mock<IRatingRankingRebuildScheduler>(MockBehavior.Strict);
        scheduler
            .Setup(value => value.ScheduleIfOutstandingAsync(first, CancellationToken.None))
            .ThrowsAsync(new InvalidOperationException("Queue unavailable"));
        scheduler
            .Setup(value => value.ScheduleIfOutstandingAsync(second, CancellationToken.None))
            .ReturnsAsync(RatingRankingRebuildScheduleDisposition.Scheduled);
        Mock<IRatingRankingSourceRevisionRepository> revisions =
            new Mock<IRatingRankingSourceRevisionRepository>(MockBehavior.Strict);
        revisions
            .Setup(value => value.CompleteMutationAsync(firstLease, true, CancellationToken.None))
            .ReturnsAsync(first);
        revisions
            .Setup(value => value.CompleteMutationAsync(secondLease, true, CancellationToken.None))
            .ReturnsAsync(second);
        revisions
            .Setup(value => value.MarkCacheConvergedAsync(
                first.ScopeKey,
                itemMethodologyVersion,
                first.Revision,
                CancellationToken.None))
            .Returns(Task.CompletedTask);
        revisions
            .Setup(value => value.MarkCacheConvergedAsync(
                second.ScopeKey,
                CanonicalRankingScopes.GlobalParks.MethodologyVersion,
                second.Revision,
                CancellationToken.None))
            .Returns(Task.CompletedTask);
        Mock<IRatingRankingPublicationCacheInvalidator> cacheInvalidator =
            new Mock<IRatingRankingPublicationCacheInvalidator>(MockBehavior.Strict);
        cacheInvalidator
            .Setup(value => value.InvalidateAsync(CancellationToken.None))
            .ReturnsAsync(true);
        RatingRankingSourceRevisionGuard guard = CreateGuard(
            revisions.Object,
            scheduler.Object,
            cacheInvalidator.Object);

        await guard.CompleteMutationAsync(
            new RatingRankingMutationPreparation(new[] { firstLease, secondLease }),
            sourceChanged: true,
            CancellationToken.None);

        scheduler.VerifyAll();
        cacheInvalidator.VerifyAll();
        revisions.VerifyAll();
    }

    [Fact]
    public async Task CompleteMutationAsync_WhenAnotherMutationIsPending_ShouldKeepRevisionHidden()
    {
        RankingScopeKey scopeKey = RankingScopeKey.Parse("parks:global");
        RatingRankingSourceRevision blockedRevision = new RatingRankingSourceRevision(
            scopeKey,
            7,
            NowUtc,
            PendingMutationCount: 1,
            MutationLeaseExpiresAtUtc: NowUtc.AddMinutes(30));
        RatingRankingMutationLease mutationLease = CreateLease(scopeKey);
        Mock<IRatingRankingSourceRevisionRepository> revisions =
            new Mock<IRatingRankingSourceRevisionRepository>(MockBehavior.Strict);
        revisions
            .Setup(value => value.CompleteMutationAsync(mutationLease, true, CancellationToken.None))
            .ReturnsAsync(blockedRevision);
        Mock<IRatingRankingRebuildScheduler> scheduler =
            new Mock<IRatingRankingRebuildScheduler>(MockBehavior.Strict);
        Mock<IRatingRankingPublicationCacheInvalidator> cacheInvalidator =
            new Mock<IRatingRankingPublicationCacheInvalidator>(MockBehavior.Strict);
        cacheInvalidator
            .Setup(value => value.InvalidateAsync(CancellationToken.None))
            .ReturnsAsync(true);
        RatingRankingSourceRevisionGuard guard = CreateGuard(
            revisions.Object,
            scheduler.Object,
            cacheInvalidator.Object);

        await guard.CompleteMutationAsync(
            new RatingRankingMutationPreparation(new[] { mutationLease }),
            sourceChanged: true,
            CancellationToken.None);

        revisions.VerifyAll();
        cacheInvalidator.VerifyAll();
        scheduler.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task PrepareParkChangesAsync_WhenVisibleParkBecomesHidden_ShouldInvalidateAllCanonicalScopes()
    {
        List<RankingScopeKey> incrementedScopes = new List<RankingScopeKey>();
        Mock<IRatingRankingSourceRevisionRepository> revisions =
            new Mock<IRatingRankingSourceRevisionRepository>(MockBehavior.Strict);
        revisions
            .Setup(repository => repository.BeginMutationAsync(
                It.IsAny<RankingScopeKey>(),
                CancellationToken.None))
            .Callback((RankingScopeKey scopeKey, CancellationToken _) => incrementedScopes.Add(scopeKey))
            .Returns((RankingScopeKey scopeKey, CancellationToken _) =>
                Task.FromResult(CreateLease(scopeKey)));
        ShareSourceMutationLease catalogLease = new ShareSourceMutationLease(
            "personal-ranking:public-catalog",
            6.ToString("x32"));
        List<string> publicCatalogScopes = new List<string>();
        Mock<IShareSourceRevisionRepository> shareRevisions =
            new Mock<IShareSourceRevisionRepository>(MockBehavior.Strict);
        shareRevisions.Setup(value => value.BeginMutationAsync(
                "personal-ranking:public-catalog",
                CancellationToken.None))
            .ReturnsAsync(catalogLease);
        shareRevisions.Setup(value => value.BeginMutationAsync(
                It.Is<string>(scope => scope != PersonalRankingShareSourceScope.PublicCatalog),
                CancellationToken.None))
            .Callback((string scope, CancellationToken _) => publicCatalogScopes.Add(scope))
            .Returns((string scope, CancellationToken _) =>
                Task.FromResult(new ShareSourceMutationLease(scope, 7.ToString("x32"))));
        RatingRankingSourceRevisionGuard guard = CreateGuard(
            revisions.Object,
            shareSourceRevisions: shareRevisions.Object);
        Park previous = new Park
        {
            Id = "park-1",
            Name = "Demo Park",
            IsVisible = true,
            Status = ParkStatus.Operating,
        };
        Park current = new Park
        {
            Id = "park-1",
            Name = "Demo Park",
            IsVisible = false,
            Status = ParkStatus.Operating,
        };

        RatingRankingMutationPreparation preparation = await guard.PrepareParkChangesAsync(
            new[] { previous },
            new[] { current },
            CancellationToken.None);

        Assert.Equal(CanonicalRankingScopes.All.Count, preparation.MutationLeases.Count);
        Assert.Same(catalogLease, preparation.PersonalRankingCatalogMutationLease);
        Assert.Equal(4, preparation.PublicCatalogMutationLeases.Count);
        Assert.Equal(
            new[]
            {
                PublicCatalogShareSourceScope.CreatePassportActivityPark("park-1"),
                PublicCatalogShareSourceScope.CreatePassportGeographyPark("park-1"),
                PublicCatalogShareSourceScope.CreatePassportMissedItemsPark("park-1"),
                PublicCatalogShareSourceScope.CreatePassportRatingsPark("park-1"),
            }.OrderBy(static scope => scope, StringComparer.Ordinal),
            publicCatalogScopes);
        Assert.Equal(
            CanonicalRankingScopes.All.Select(static scope => scope.Key.Value).OrderBy(static key => key),
            incrementedScopes.Select(static scope => scope.Value));
        revisions.VerifyAll();
        shareRevisions.VerifyAll();
    }

    [Fact]
    public async Task PrepareParkChangesAsync_WhenIncludedParkNameChanges_ShouldInvalidateOnlyParkScope()
    {
        List<RankingScopeKey> incrementedScopes = new List<RankingScopeKey>();
        Mock<IRatingRankingSourceRevisionRepository> revisions =
            new Mock<IRatingRankingSourceRevisionRepository>(MockBehavior.Strict);
        revisions
            .Setup(repository => repository.BeginMutationAsync(
                It.IsAny<RankingScopeKey>(),
                CancellationToken.None))
            .Callback((RankingScopeKey scopeKey, CancellationToken _) => incrementedScopes.Add(scopeKey))
            .Returns((RankingScopeKey scopeKey, CancellationToken _) =>
                Task.FromResult(CreateLease(scopeKey)));
        RatingRankingSourceRevisionGuard guard = CreateGuard(revisions.Object);
        Park previous = new Park
        {
            Id = "park-1",
            Name = "Alpha Park",
            IsVisible = true,
            Status = ParkStatus.Operating,
        };
        Park current = new Park
        {
            Id = "park-1",
            Name = "Beta Park",
            IsVisible = true,
            Status = ParkStatus.Operating,
        };

        RatingRankingMutationPreparation preparation = await guard.PrepareParkChangesAsync(
            new[] { previous },
            new[] { current },
            CancellationToken.None);

        RankingScopeKey scopeKey = Assert.Single(preparation.MutationLeases).ScopeKey;
        Assert.Equal("parks:global", scopeKey.Value);
        Assert.Equal(new[] { "parks:global" }, incrementedScopes.Select(static scope => scope.Value));
        revisions.VerifyAll();
    }

    [Fact]
    public async Task PrepareParkChangesAsync_WhenOnlyPublicLabelCasingChanges_ShouldFenceShareCatalogOnly()
    {
        Mock<IRatingRankingSourceRevisionRepository> revisions =
            new Mock<IRatingRankingSourceRevisionRepository>(MockBehavior.Strict);
        ShareSourceMutationLease catalogLease = new ShareSourceMutationLease(
            "personal-ranking:public-catalog",
            10.ToString("x32"));
        List<string> publicCatalogScopes = new List<string>();
        Mock<IShareSourceRevisionRepository> shareRevisions =
            new Mock<IShareSourceRevisionRepository>(MockBehavior.Strict);
        shareRevisions.Setup(value => value.BeginMutationAsync(
                "personal-ranking:public-catalog",
                CancellationToken.None))
            .ReturnsAsync(catalogLease);
        shareRevisions.Setup(value => value.BeginMutationAsync(
                It.Is<string>(scope => scope != PersonalRankingShareSourceScope.PublicCatalog),
                CancellationToken.None))
            .Callback((string scope, CancellationToken _) => publicCatalogScopes.Add(scope))
            .Returns((string scope, CancellationToken _) =>
                Task.FromResult(new ShareSourceMutationLease(scope, 11.ToString("x32"))));
        RatingRankingSourceRevisionGuard guard = CreateGuard(
            revisions.Object,
            shareSourceRevisions: shareRevisions.Object);
        Park previous = new Park
        {
            Id = "park-1",
            Name = "Demo Park",
            IsVisible = true,
            Status = ParkStatus.Operating,
        };
        Park current = new Park
        {
            Id = "park-1",
            Name = "DEMO PARK",
            IsVisible = true,
            Status = ParkStatus.Operating,
        };

        RatingRankingMutationPreparation preparation = await guard.PrepareParkChangesAsync(
            new[] { previous },
            new[] { current },
            CancellationToken.None);

        Assert.Empty(preparation.MutationLeases);
        Assert.Same(catalogLease, preparation.PersonalRankingCatalogMutationLease);
        Assert.Equal(
            new[]
            {
                PublicCatalogShareSourceScope.CreatePassportGeographyPark("park-1"),
                PublicCatalogShareSourceScope.CreatePassportRatingsPark("park-1"),
            },
            publicCatalogScopes);
        revisions.VerifyNoOtherCalls();
        shareRevisions.VerifyAll();
    }

    [Fact]
    public async Task PrepareParkChangesAsync_WhenVisibleParkStopsBeingRatingEligible_ShouldFenceItsPublicShareCatalog()
    {
        Mock<IRatingRankingSourceRevisionRepository> revisions =
            new Mock<IRatingRankingSourceRevisionRepository>(MockBehavior.Strict);
        revisions
            .Setup(repository => repository.BeginMutationAsync(
                It.IsAny<RankingScopeKey>(),
                CancellationToken.None))
            .Returns((RankingScopeKey scopeKey, CancellationToken _) =>
                Task.FromResult(CreateLease(scopeKey)));
        ShareSourceMutationLease catalogLease = new ShareSourceMutationLease(
            PersonalRankingShareSourceScope.PublicCatalog,
            8.ToString("x32"));
        string parkCatalogScope =
            PublicCatalogShareSourceScope.CreatePassportRatingsPark("park-1");
        ShareSourceMutationLease parkCatalogLease = new ShareSourceMutationLease(
            parkCatalogScope,
            9.ToString("x32"));
        Mock<IShareSourceRevisionRepository> shareRevisions =
            new Mock<IShareSourceRevisionRepository>(MockBehavior.Strict);
        shareRevisions.Setup(value => value.BeginMutationAsync(
                PersonalRankingShareSourceScope.PublicCatalog,
                CancellationToken.None))
            .ReturnsAsync(catalogLease);
        shareRevisions.Setup(value => value.BeginMutationAsync(
                parkCatalogScope,
                CancellationToken.None))
            .ReturnsAsync(parkCatalogLease);
        RatingRankingSourceRevisionGuard guard = CreateGuard(
            revisions.Object,
            shareSourceRevisions: shareRevisions.Object);
        Park previous = new Park
        {
            Id = "park-1",
            Name = "Demo Park",
            CountryCode = "FR",
            IsVisible = true,
            Status = ParkStatus.Operating,
        };
        Park current = new Park
        {
            Id = "park-1",
            Name = "Demo Park",
            CountryCode = "FR",
            IsVisible = true,
            Status = ParkStatus.ClosedDefinitively,
        };

        RatingRankingMutationPreparation preparation = await guard.PrepareParkChangesAsync(
            new[] { previous },
            new[] { current },
            CancellationToken.None);

        Assert.Equal(CanonicalRankingScopes.All.Count, preparation.MutationLeases.Count);
        Assert.Same(catalogLease, preparation.PersonalRankingCatalogMutationLease);
        Assert.Same(parkCatalogLease, Assert.Single(preparation.PublicCatalogMutationLeases));
        revisions.VerifyAll();
        shareRevisions.VerifyAll();
    }

    [Fact]
    public async Task PrepareParkChangesAsync_WhenVisibleClosedParkNameChanges_ShouldFenceShareCatalog()
    {
        Mock<IRatingRankingSourceRevisionRepository> revisions =
            new Mock<IRatingRankingSourceRevisionRepository>(MockBehavior.Strict);
        ShareSourceMutationLease catalogLease = new ShareSourceMutationLease(
            PersonalRankingShareSourceScope.PublicCatalog,
            13.ToString("x32"));
        string parkCatalogScope =
            PublicCatalogShareSourceScope.CreatePassportGeographyPark("closed-park");
        ShareSourceMutationLease parkCatalogLease = new ShareSourceMutationLease(
            parkCatalogScope,
            14.ToString("x32"));
        Mock<IShareSourceRevisionRepository> shareRevisions =
            new Mock<IShareSourceRevisionRepository>(MockBehavior.Strict);
        shareRevisions.Setup(value => value.BeginMutationAsync(
                PersonalRankingShareSourceScope.PublicCatalog,
                CancellationToken.None))
            .ReturnsAsync(catalogLease);
        shareRevisions.Setup(value => value.BeginMutationAsync(
                parkCatalogScope,
                CancellationToken.None))
            .ReturnsAsync(parkCatalogLease);
        RatingRankingSourceRevisionGuard guard = CreateGuard(
            revisions.Object,
            shareSourceRevisions: shareRevisions.Object);
        Park previous = new Park
        {
            Id = "closed-park",
            Name = "Ancien nom",
            IsVisible = true,
            Status = ParkStatus.ClosedDefinitively,
        };
        Park current = new Park
        {
            Id = "closed-park",
            Name = "Nouveau nom",
            IsVisible = true,
            Status = ParkStatus.ClosedDefinitively,
        };

        RatingRankingMutationPreparation preparation = await guard.PrepareParkChangesAsync(
            new[] { previous },
            new[] { current },
            CancellationToken.None);

        Assert.Empty(preparation.MutationLeases);
        Assert.Same(catalogLease, preparation.PersonalRankingCatalogMutationLease);
        Assert.Same(parkCatalogLease, Assert.Single(preparation.PublicCatalogMutationLeases));
        Assert.Same(parkCatalogLease, Assert.Single(preparation.PublicCatalogMutationLeases));
        revisions.VerifyNoOtherCalls();
        shareRevisions.VerifyAll();
    }

    [Fact]
    public async Task PrepareParkItemChangesAsync_WhenCategoryChanges_ShouldInvalidateOldNewAndParkScopes()
    {
        List<RankingScopeKey> incrementedScopes = new List<RankingScopeKey>();
        Mock<IRatingRankingSourceRevisionRepository> revisions =
            new Mock<IRatingRankingSourceRevisionRepository>(MockBehavior.Strict);
        revisions
            .Setup(repository => repository.BeginMutationAsync(
                It.IsAny<RankingScopeKey>(),
                CancellationToken.None))
            .Callback((RankingScopeKey scopeKey, CancellationToken _) => incrementedScopes.Add(scopeKey))
            .Returns((RankingScopeKey scopeKey, CancellationToken _) =>
                Task.FromResult(CreateLease(scopeKey)));
        RatingRankingSourceRevisionGuard guard = CreateGuard(revisions.Object);
        ParkItem previous = CreateVisibleParkItem(ParkItemCategory.Attraction);
        ParkItem current = CreateVisibleParkItem(ParkItemCategory.Show);

        RatingRankingMutationPreparation preparation = await guard.PrepareParkItemChangesAsync(
            new[] { previous },
            new[] { current },
            CancellationToken.None);

        Assert.Equal(
            new[] { "park-items:category:attraction", "park-items:category:show", "parks:global" },
            incrementedScopes.Select(static scope => scope.Value));
        Assert.Equal(3, preparation.MutationLeases.Count);
        revisions.VerifyAll();
    }

    [Fact]
    public async Task PrepareParkItemChangesAsync_WhenIncludedItemNameChanges_ShouldInvalidateOnlyItsCategoryScope()
    {
        List<RankingScopeKey> incrementedScopes = new List<RankingScopeKey>();
        Mock<IRatingRankingSourceRevisionRepository> revisions =
            new Mock<IRatingRankingSourceRevisionRepository>(MockBehavior.Strict);
        revisions
            .Setup(repository => repository.BeginMutationAsync(
                It.IsAny<RankingScopeKey>(),
                CancellationToken.None))
            .Callback((RankingScopeKey scopeKey, CancellationToken _) => incrementedScopes.Add(scopeKey))
            .Returns((RankingScopeKey scopeKey, CancellationToken _) =>
                Task.FromResult(CreateLease(scopeKey)));
        RatingRankingSourceRevisionGuard guard = CreateGuard(revisions.Object);
        ParkItem previous = CreateVisibleParkItem(ParkItemCategory.Attraction);
        previous.Name = "Alpha Ride";
        ParkItem current = CreateVisibleParkItem(ParkItemCategory.Attraction);
        current.Name = "Beta Ride";

        RatingRankingMutationPreparation preparation = await guard.PrepareParkItemChangesAsync(
            new[] { previous },
            new[] { current },
            CancellationToken.None);

        RankingScopeKey scopeKey = Assert.Single(preparation.MutationLeases).ScopeKey;
        Assert.Equal("park-items:category:attraction", scopeKey.Value);
        Assert.Equal(
            new[] { "park-items:category:attraction" },
            incrementedScopes.Select(static scope => scope.Value));
        revisions.VerifyAll();
    }

    [Fact]
    public async Task PrepareParkItemChangesAsync_WhenOnlyPublicLabelCasingChanges_ShouldFenceShareCatalogOnly()
    {
        Mock<IRatingRankingSourceRevisionRepository> revisions =
            new Mock<IRatingRankingSourceRevisionRepository>(MockBehavior.Strict);
        ShareSourceMutationLease catalogLease = new ShareSourceMutationLease(
            "personal-ranking:public-catalog",
            11.ToString("x32"));
        List<string> publicCatalogScopes = new List<string>();
        Mock<IShareSourceRevisionRepository> shareRevisions =
            new Mock<IShareSourceRevisionRepository>(MockBehavior.Strict);
        shareRevisions.Setup(value => value.BeginMutationAsync(
                "personal-ranking:public-catalog",
                CancellationToken.None))
            .ReturnsAsync(catalogLease);
        shareRevisions.Setup(value => value.BeginMutationAsync(
                It.Is<string>(scope => scope != PersonalRankingShareSourceScope.PublicCatalog),
                CancellationToken.None))
            .Callback((string scope, CancellationToken _) => publicCatalogScopes.Add(scope))
            .Returns((string scope, CancellationToken _) =>
                Task.FromResult(new ShareSourceMutationLease(scope, 12.ToString("x32"))));
        RatingRankingSourceRevisionGuard guard = CreateGuard(
            revisions.Object,
            shareSourceRevisions: shareRevisions.Object);
        ParkItem previous = CreateVisibleParkItem(ParkItemCategory.Attraction);
        previous.Name = "Demo Ride";
        ParkItem current = CreateVisibleParkItem(ParkItemCategory.Attraction);
        current.Name = "DEMO RIDE";

        RatingRankingMutationPreparation preparation = await guard.PrepareParkItemChangesAsync(
            new[] { previous },
            new[] { current },
            CancellationToken.None);

        Assert.Empty(preparation.MutationLeases);
        Assert.Same(catalogLease, preparation.PersonalRankingCatalogMutationLease);
        Assert.Equal(
            new[]
            {
                PublicCatalogShareSourceScope.CreatePassportMissedItemsPark("park-1"),
                PublicCatalogShareSourceScope.CreatePassportRatingsPark("park-1"),
            },
            publicCatalogScopes);
        revisions.VerifyNoOtherCalls();
        shareRevisions.VerifyAll();
    }

    [Fact]
    public async Task PrepareParkItemChangesAsync_WhenVisibleClosedItemNameChanges_ShouldFenceShareCatalog()
    {
        Mock<IRatingRankingSourceRevisionRepository> revisions =
            new Mock<IRatingRankingSourceRevisionRepository>(MockBehavior.Strict);
        ShareSourceMutationLease catalogLease = new ShareSourceMutationLease(
            PersonalRankingShareSourceScope.PublicCatalog,
            14.ToString("x32"));
        string parkCatalogScope =
            PublicCatalogShareSourceScope.CreatePassportMissedItemsPark("park-1");
        ShareSourceMutationLease parkCatalogLease = new ShareSourceMutationLease(
            parkCatalogScope,
            15.ToString("x32"));
        Mock<IShareSourceRevisionRepository> shareRevisions =
            new Mock<IShareSourceRevisionRepository>(MockBehavior.Strict);
        shareRevisions.Setup(value => value.BeginMutationAsync(
                PersonalRankingShareSourceScope.PublicCatalog,
                CancellationToken.None))
            .ReturnsAsync(catalogLease);
        shareRevisions.Setup(value => value.BeginMutationAsync(
                parkCatalogScope,
                CancellationToken.None))
            .ReturnsAsync(parkCatalogLease);
        RatingRankingSourceRevisionGuard guard = CreateGuard(
            revisions.Object,
            shareSourceRevisions: shareRevisions.Object);
        ParkItem previous = CreateVisibleParkItem(ParkItemCategory.Attraction);
        previous.Name = "Ancien nom";
        previous.AttractionDetails!.Status = ParkItemStatusNormalizer.ClosedDefinitively;
        ParkItem current = CreateVisibleParkItem(ParkItemCategory.Attraction);
        current.Name = "Nouveau nom";
        current.AttractionDetails!.Status = ParkItemStatusNormalizer.ClosedDefinitively;

        RatingRankingMutationPreparation preparation = await guard.PrepareParkItemChangesAsync(
            new[] { previous },
            new[] { current },
            CancellationToken.None);

        Assert.Empty(preparation.MutationLeases);
        Assert.Same(catalogLease, preparation.PersonalRankingCatalogMutationLease);
        Assert.Same(parkCatalogLease, Assert.Single(preparation.PublicCatalogMutationLeases));
        Assert.Same(parkCatalogLease, Assert.Single(preparation.PublicCatalogMutationLeases));
        revisions.VerifyNoOtherCalls();
        shareRevisions.VerifyAll();
    }

    [Fact]
    public async Task PrepareParkItemChangesAsync_WhenIncludedItemTypeChanges_ShouldInvalidateOnlyParkScope()
    {
        List<RankingScopeKey> incrementedScopes = new List<RankingScopeKey>();
        Mock<IRatingRankingSourceRevisionRepository> revisions =
            new Mock<IRatingRankingSourceRevisionRepository>(MockBehavior.Strict);
        revisions
            .Setup(repository => repository.BeginMutationAsync(
                It.IsAny<RankingScopeKey>(),
                CancellationToken.None))
            .Callback((RankingScopeKey scopeKey, CancellationToken _) => incrementedScopes.Add(scopeKey))
            .Returns((RankingScopeKey scopeKey, CancellationToken _) =>
                Task.FromResult(CreateLease(scopeKey)));
        RatingRankingSourceRevisionGuard guard = CreateGuard(revisions.Object);
        ParkItem previous = CreateVisibleParkItem(ParkItemCategory.Attraction);
        previous.Type = ParkItemType.RollerCoaster;
        ParkItem current = CreateVisibleParkItem(ParkItemCategory.Attraction);
        current.Type = ParkItemType.DarkRide;

        RatingRankingMutationPreparation preparation = await guard.PrepareParkItemChangesAsync(
            new[] { previous },
            new[] { current },
            CancellationToken.None);

        RankingScopeKey scopeKey = Assert.Single(preparation.MutationLeases).ScopeKey;
        Assert.Equal("parks:global", scopeKey.Value);
        Assert.Equal(new[] { "parks:global" }, incrementedScopes.Select(static scope => scope.Value));
        revisions.VerifyAll();
    }

    [Fact]
    public async Task PrepareParkItemChangesAsync_WhenHiddenItemMetadataChanges_ShouldNotAdvanceRevision()
    {
        Mock<IRatingRankingSourceRevisionRepository> revisions =
            new Mock<IRatingRankingSourceRevisionRepository>(MockBehavior.Strict);
        RatingRankingSourceRevisionGuard guard = CreateGuard(revisions.Object);
        ParkItem previous = CreateVisibleParkItem(ParkItemCategory.Attraction);
        previous.IsVisible = false;
        ParkItem current = CreateVisibleParkItem(ParkItemCategory.Show);
        current.IsVisible = false;

        RatingRankingMutationPreparation preparation = await guard.PrepareParkItemChangesAsync(
            new[] { previous },
            new[] { current },
            CancellationToken.None);

        Assert.Empty(preparation.MutationLeases);
        revisions.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CompleteMutationAsync_ShouldSettleEveryParkScopedCatalogLease()
    {
        ShareSourceMutationLease parkCatalogLease = new ShareSourceMutationLease(
            PublicCatalogShareSourceScope.CreatePassportActivityPark("park-1"),
            16.ToString("x32"));
        Mock<IShareSourceRevisionRepository> shareRevisions =
            new Mock<IShareSourceRevisionRepository>(MockBehavior.Strict);
        shareRevisions.Setup(value => value.CompleteMutationAsync(
                parkCatalogLease,
                true,
                CancellationToken.None))
            .ReturnsAsync(new ShareSourceRevision(2, 0, DateTime.UtcNow));
        Mock<IRatingRankingSourceRevisionRepository> revisions =
            new Mock<IRatingRankingSourceRevisionRepository>(MockBehavior.Strict);
        RatingRankingSourceRevisionGuard guard = CreateGuard(
            revisions.Object,
            shareSourceRevisions: shareRevisions.Object);
        RatingRankingMutationPreparation preparation = new RatingRankingMutationPreparation(
            Array.Empty<RatingRankingMutationLease>(),
            publicCatalogMutationLeases: new[] { parkCatalogLease });

        await guard.CompleteMutationAsync(
            preparation,
            sourceChanged: true,
            CancellationToken.None);

        revisions.VerifyNoOtherCalls();
        shareRevisions.VerifyAll();
    }

    private static RatingRankingSourceRevisionGuard CreateGuard(
        IRatingRankingSourceRevisionRepository revisions,
        IRatingRankingRebuildScheduler? scheduler = null,
        IRatingRankingPublicationCacheInvalidator? cacheInvalidator = null,
        IShareSourceRevisionRepository? shareSourceRevisions = null)
    {
        RankingScopeRegistry registry = new RankingScopeRegistry(
            CanonicalRankingScopes.Version,
            CanonicalRankingScopes.All);
        IRatingRankingRebuildScheduler resolvedScheduler = scheduler
            ?? new Mock<IRatingRankingRebuildScheduler>(MockBehavior.Strict).Object;
        IRatingRankingPublicationCacheInvalidator resolvedCacheInvalidator = cacheInvalidator
            ?? new Mock<IRatingRankingPublicationCacheInvalidator>(MockBehavior.Strict).Object;
        IShareSourceRevisionRepository resolvedShareSourceRevisions =
            shareSourceRevisions ?? CreateShareSourceRevisionRepository();
        return new RatingRankingSourceRevisionGuard(
            registry,
            revisions,
            resolvedShareSourceRevisions,
            resolvedScheduler,
            resolvedCacheInvalidator,
            NullLogger<RatingRankingSourceRevisionGuard>.Instance);
    }

    private static IShareSourceRevisionRepository CreateShareSourceRevisionRepository()
    {
        Mock<IShareSourceRevisionRepository> repository =
            new Mock<IShareSourceRevisionRepository>(MockBehavior.Loose);
        repository.Setup(value => value.BeginMutationAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns((string scopeKey, CancellationToken _) =>
                Task.FromResult(new ShareSourceMutationLease(scopeKey, 7.ToString("x32"))));
        repository.Setup(value => value.CompleteMutationAsync(
                It.IsAny<ShareSourceMutationLease>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ShareSourceRevision(1, 0, DateTime.UtcNow));
        return repository.Object;
    }

    private static ParkItem CreateVisibleParkItem(ParkItemCategory category)
    {
        return new ParkItem
        {
            Id = "item-1",
            ParkId = "park-1",
            Name = "Demo Item",
            Category = category,
            IsVisible = true,
            AttractionDetails = new AttractionDetails
            {
                Status = ParkItemStatusNormalizer.Operating,
            },
        };
    }

    private static RatingRankingMutationLease CreateLease(
        RankingScopeKey scopeKey,
        int tokenSeed = 1)
    {
        return new RatingRankingMutationLease(
            scopeKey,
            tokenSeed.ToString("x32"));
    }

    private static RatingRankingMutationRecoveryTarget CreateRecoveryTarget(
        RatingTargetType targetType,
        string targetId)
    {
        return new RatingRankingMutationRecoveryTarget(
            targetType,
            targetId,
            "user-1",
            9.ToString("x32"));
    }
}
