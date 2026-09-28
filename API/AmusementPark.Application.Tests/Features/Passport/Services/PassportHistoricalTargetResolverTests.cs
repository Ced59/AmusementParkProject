using AmusementPark.Application.Features.History.Handlers;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.History.Services;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Application.Features.Passport.Services;
using AmusementPark.Application.Tests.Features.History.Handlers;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Visits;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Passport.Services;

public sealed class PassportHistoricalTargetResolverTests
{
    [Fact]
    public async Task ResolveRecordedAsync_ShouldMarkEveryCurrentFallbackAsValidationOnly()
    {
        const string ParkId = "park-1";
        const string ParkItemId = "hidden-ride";
        const string VisibleParkItemId = "visible-ride";
        Mock<IParkRepository> parks = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> parkItems = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<IParkZoneRepository> zones = new Mock<IParkZoneRepository>(MockBehavior.Strict);
        Mock<IHistoricalFactRepository> facts =
            new Mock<IHistoricalFactRepository>(MockBehavior.Strict);
        parks.Setup(repository => repository.GetByIdAsync(
                ParkId,
                false,
                CancellationToken.None))
            .ReturnsAsync((Park?)null);
        Mock<IVisitTargetResolver> currentTargets =
            new Mock<IVisitTargetResolver>(MockBehavior.Strict);
        currentTargets.Setup(resolver => resolver.ResolveAsync(
                It.IsAny<IReadOnlyCollection<string>>(),
                CancellationToken.None))
            .ReturnsAsync((IReadOnlyCollection<string> ids, CancellationToken _) => ids.ToDictionary(
                static id => id,
                id => new VisitTarget(
                    id,
                    ParkId,
                    id == ParkItemId ? "Attraction masquée" : "Attraction visible",
                    ParkItemCategory.Attraction,
                    null,
                    null,
                    null,
                    id == VisibleParkItemId),
                StringComparer.Ordinal));
        Mock<IImageRepository> images = new Mock<IImageRepository>(MockBehavior.Strict);
        PassportHistoricalTargetResolver resolver = new PassportHistoricalTargetResolver(
            new PublicParkHistoricalDataLoader(
                parks.Object,
                parkItems.Object,
                zones.Object,
                facts.Object,
                CreateRolloutGateAssessmentService()),
            new ParkHistoricalSnapshotBuilder(),
            currentTargets.Object,
            images.Object);
        Visit visit = Visit.Create(
            VisitId.Parse("visit-1"),
            "owner-1",
            ParkId,
            VisitDate.ForYear(2020),
            null,
            LocalServiceDayConvention.VisitStartLocalDate,
            null,
            null,
            new DateTime(2026, 9, 27, 8, 0, 0, DateTimeKind.Utc));

        PassportHistoricalTargetContext recorded = await resolver.ResolveRecordedAsync(
            visit,
            new[] { ParkItemId },
            CancellationToken.None);
        PassportHistoricalTargetContext newEntry = await resolver.ResolveAsync(
            visit,
            new[] { ParkItemId },
            CancellationToken.None);
        PassportHistoricalTargetContext visibleRecorded = await resolver.ResolveRecordedAsync(
            visit,
            new[] { VisibleParkItemId },
            CancellationToken.None);
        PassportHistoricalTargetContext visibleNewEntry = await resolver.ResolveAsync(
            visit,
            new[] { VisibleParkItemId },
            CancellationToken.None);

        PassportHistoricalTarget target = Assert.Single(recorded.Targets).Value;
        Assert.Equal("Attraction masquée", target.Name);
        Assert.True(target.IsHistoricalOnly);
        Assert.True(target.IsValidationFallback);
        Assert.False(target.HasCanonicalEvidence);
        Assert.Empty(newEntry.Targets);
        Assert.True(Assert.Single(visibleRecorded.Targets).Value.IsValidationFallback);
        Assert.False(Assert.Single(visibleNewEntry.Targets).Value.IsValidationFallback);
        Assert.False(Assert.Single(visibleNewEntry.Targets).Value.HasCanonicalEvidence);
        parks.VerifyAll();
        currentTargets.VerifyAll();
        parkItems.VerifyNoOtherCalls();
        zones.VerifyNoOtherCalls();
        facts.VerifyNoOtherCalls();
        images.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ResolveManyAsync_ShouldHydrateCanonicalHistoryOnceAndBoundStoredNames()
    {
        Park park = PublicParkHistoryTestData.CreatePark();
        string currentName = new string('N', 250);
        ParkItem item = PublicParkHistoryTestData.CreateParkItem("ride-1", currentName);
        item.Category = ParkItemCategory.Attraction;
        HistoricalSubject subject = new HistoricalSubject(
            HistoricalSubjectType.ParkItem,
            item.Id,
            "Nom historique",
            HistoricalSubjectPublicationPolicy.FollowCurrentSubject,
            park.Id);
        HistoricalFact opening = PublicParkHistoryTestData.CreateOpeningFact(subject, 2000);
        Mock<IParkRepository> parks = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> parkItems = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<IParkZoneRepository> zones = new Mock<IParkZoneRepository>(MockBehavior.Strict);
        Mock<IHistoricalFactRepository> facts =
            new Mock<IHistoricalFactRepository>(MockBehavior.Strict);
        parks.Setup(repository => repository.GetByIdAsync(
                park.Id,
                false,
                CancellationToken.None))
            .ReturnsAsync(park);
        parkItems.Setup(repository => repository.GetByIdsAsync(
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { item.Id })),
                CancellationToken.None))
            .ReturnsAsync(new[] { item });
        facts.Setup(repository => repository.GetLatestRevisionsForSubjectsAsync(
                It.IsAny<IReadOnlyCollection<HistoricalSubject>>(),
                CancellationToken.None))
            .ReturnsAsync(new[] { opening });
        Mock<IVisitTargetResolver> currentTargets =
            new Mock<IVisitTargetResolver>(MockBehavior.Strict);
        currentTargets.Setup(resolver => resolver.ResolveAsync(
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { item.Id })),
                CancellationToken.None))
            .ReturnsAsync(new Dictionary<string, VisitTarget>(StringComparer.Ordinal)
            {
                [item.Id] = new VisitTarget(
                    item.Id,
                    park.Id,
                    item.Name,
                    ParkItemCategory.Attraction,
                    null,
                    null),
            });
        Mock<IImageRepository> images = new Mock<IImageRepository>(MockBehavior.Strict);
        PassportHistoricalTargetResolver resolver = new PassportHistoricalTargetResolver(
            new PublicParkHistoricalDataLoader(
                parks.Object,
                parkItems.Object,
                zones.Object,
                facts.Object,
                CreateRolloutGateAssessmentService()),
            new ParkHistoricalSnapshotBuilder(),
            currentTargets.Object,
            images.Object);
        Visit visit = Visit.Create(
            VisitId.Parse("visit-1"),
            "owner-1",
            park.Id,
            VisitDate.ForYear(1990),
            null,
            LocalServiceDayConvention.VisitStartLocalDate,
            null,
            null,
            new DateTime(2026, 9, 27, 8, 0, 0, DateTimeKind.Utc));

        IReadOnlyDictionary<VisitDate, PassportHistoricalTargetContext> contexts =
            await resolver.ResolveManyAsync(
            park.Id,
            new[] { visit.Date, VisitDate.ForYear(2005) },
            new[] { item.Id },
            CancellationToken.None);

        PassportHistoricalTarget target = Assert.Single(contexts[visit.Date].Targets).Value;
        Assert.Equal(currentName, target.Name);
        Assert.Equal(HistoricalTargetReference.MaximumNameLength, target.HistoricalTarget.Name.Length);
        Assert.Equal(HistoricalOperationalState.KnownClosed, target.OperationalState);
        Assert.Equal(HistoricalConsistency.ConfirmedConflict, target.HistoricalConsistency);
        Assert.True(target.HasCanonicalEvidence);
        Assert.False(target.HasCanonicalNameEvidence);
        Assert.False(target.HasCanonicalClassificationEvidence);
        Assert.Null(target.MainImageId);
        PassportHistoricalTarget laterTarget = Assert.Single(
            contexts[VisitDate.ForYear(2005)].Targets).Value;
        Assert.Equal(HistoricalOperationalState.KnownOpen, laterTarget.OperationalState);
        Assert.True(laterTarget.HasCanonicalEvidence);
        Assert.False(laterTarget.HasCanonicalNameEvidence);
        Assert.False(laterTarget.HasCanonicalClassificationEvidence);
        parks.VerifyAll();
        parkItems.VerifyAll();
        facts.VerifyAll();
        currentTargets.VerifyAll();
        parks.Verify(repository => repository.GetByIdAsync(
            park.Id,
            false,
            CancellationToken.None), Times.Once);
        facts.Verify(repository => repository.GetLatestDecisionEligibleRevisionsForParkAsync(
            It.IsAny<string>(),
            It.IsAny<IReadOnlyCollection<HistoricalSubject>>(),
            It.IsAny<CancellationToken>()), Times.Never);
        currentTargets.Verify(resolver => resolver.ResolveAsync(
            It.IsAny<IReadOnlyCollection<string>>(),
            CancellationToken.None), Times.Once);
        zones.VerifyNoOtherCalls();
        images.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ResolveManyAsync_WithCurrentCatalogOnly_ShouldKeepContextNonCanonicalAndDetailed()
    {
        Park park = PublicParkHistoryTestData.CreatePark();
        ParkItem item = PublicParkHistoryTestData.CreateParkItem("ride-1", "Attraction actuelle");
        item.Category = ParkItemCategory.Attraction;
        item.Type = ParkItemType.DarkRide;
        Mock<IParkRepository> parks = new(MockBehavior.Strict);
        parks.Setup(repository => repository.GetByIdAsync(
                park.Id,
                false,
                CancellationToken.None))
            .ReturnsAsync(park);
        Mock<IParkItemRepository> parkItems = new(MockBehavior.Strict);
        parkItems.Setup(repository => repository.GetByIdsAsync(
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { item.Id })),
                CancellationToken.None))
            .ReturnsAsync(new[] { item });
        Mock<IParkZoneRepository> zones = new(MockBehavior.Strict);
        Mock<IHistoricalFactRepository> facts = new(MockBehavior.Strict);
        facts.Setup(repository => repository.GetLatestRevisionsForSubjectsAsync(
                It.IsAny<IReadOnlyCollection<HistoricalSubject>>(),
                CancellationToken.None))
            .ReturnsAsync(Array.Empty<HistoricalFact>());
        Mock<IVisitTargetResolver> currentTargets = new(MockBehavior.Strict);
        currentTargets.Setup(resolver => resolver.ResolveAsync(
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { item.Id })),
                CancellationToken.None))
            .ReturnsAsync(new Dictionary<string, VisitTarget>(StringComparer.Ordinal)
            {
                [item.Id] = new VisitTarget(
                    item.Id,
                    park.Id,
                    item.Name,
                    item.Category,
                    null,
                    null,
                    null,
                    true,
                    null,
                    item.Type),
            });
        Mock<IImageRepository> images = new(MockBehavior.Strict);
        PassportHistoricalTargetResolver resolver = new(
            new PublicParkHistoricalDataLoader(
                parks.Object,
                parkItems.Object,
                zones.Object,
                facts.Object,
                CreateRolloutGateAssessmentService()),
            new ParkHistoricalSnapshotBuilder(),
            currentTargets.Object,
            images.Object);

        IReadOnlyDictionary<VisitDate, PassportHistoricalTargetContext> contexts =
            await resolver.ResolveManyAsync(
                park.Id,
                new[] { VisitDate.ForYear(2005) },
                new[] { item.Id },
                CancellationToken.None);

        PassportHistoricalTarget target = Assert.Single(contexts.Values).Targets[item.Id];
        Assert.False(target.IsValidationFallback);
        Assert.False(target.HasCanonicalEvidence);
        Assert.False(target.HasCanonicalNameEvidence);
        Assert.False(target.HasCanonicalClassificationEvidence);
        Assert.Equal(ParkItemType.DarkRide.ToString(), target.HistoricalClassification);
        parks.VerifyAll();
        parkItems.VerifyAll();
        facts.VerifyAll();
        currentTargets.VerifyAll();
        zones.VerifyNoOtherCalls();
        images.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(ParkItemCategory.Restaurant)]
    [InlineData(ParkItemCategory.Other)]
    public async Task ResolveAsync_ShouldNotRestoreACanonicallyNonAttractionSubject(
        ParkItemCategory historicalCategory)
    {
        Park park = PublicParkHistoryTestData.CreatePark();
        ParkItem item = PublicParkHistoryTestData.CreateParkItem("item-1", "Ancienne attraction");
        item.Category = ParkItemCategory.Attraction;
        HistoricalSubject subject = new HistoricalSubject(
            HistoricalSubjectType.ParkItem,
            item.Id,
            item.Name,
            HistoricalSubjectPublicationPolicy.FollowCurrentSubject,
            park.Id);
        Mock<IParkRepository> parks = new Mock<IParkRepository>(MockBehavior.Strict);
        parks.Setup(repository => repository.GetByIdAsync(
                park.Id,
                false,
                CancellationToken.None))
            .ReturnsAsync(park);
        Mock<IParkItemRepository> parkItems = new Mock<IParkItemRepository>(MockBehavior.Strict);
        parkItems.Setup(repository => repository.GetByIdsAsync(
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { item.Id })),
                CancellationToken.None))
            .ReturnsAsync(new[] { item });
        Mock<IParkZoneRepository> zones = new Mock<IParkZoneRepository>(MockBehavior.Strict);
        Mock<IHistoricalFactRepository> facts =
            new Mock<IHistoricalFactRepository>(MockBehavior.Strict);
        facts.Setup(repository => repository.GetLatestRevisionsForSubjectsAsync(
                It.IsAny<IReadOnlyCollection<HistoricalSubject>>(),
                CancellationToken.None))
            .ReturnsAsync(Array.Empty<HistoricalFact>());
        Mock<IParkHistoricalSnapshotBuilder> snapshots =
            new Mock<IParkHistoricalSnapshotBuilder>(MockBehavior.Strict);
        snapshots.Setup(builder => builder.Build(
                park.Id,
                It.IsAny<HistoricalInstant>(),
                It.IsAny<IReadOnlyCollection<HistoricalSubject>>(),
                It.IsAny<IReadOnlyCollection<HistoricalFact>>()))
            .Returns((string _, HistoricalInstant instant,
                IReadOnlyCollection<HistoricalSubject> __,
                IReadOnlyCollection<HistoricalFact> ___) => CreateNonAttractionSnapshot(
                    park.Id,
                    instant,
                    subject,
                    historicalCategory));
        Mock<IVisitTargetResolver> currentTargets =
            new Mock<IVisitTargetResolver>(MockBehavior.Strict);
        currentTargets.Setup(resolver => resolver.ResolveAsync(
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { item.Id })),
                CancellationToken.None))
            .ReturnsAsync(new Dictionary<string, VisitTarget>(StringComparer.Ordinal)
            {
                [item.Id] = new VisitTarget(
                    item.Id,
                    park.Id,
                    item.Name,
                    ParkItemCategory.Attraction,
                    null,
                    null),
            });
        Mock<IImageRepository> images = new Mock<IImageRepository>(MockBehavior.Strict);
        PassportHistoricalTargetResolver resolver = new PassportHistoricalTargetResolver(
            new PublicParkHistoricalDataLoader(
                parks.Object,
                parkItems.Object,
                zones.Object,
                facts.Object,
                CreateRolloutGateAssessmentService()),
            snapshots.Object,
            currentTargets.Object,
            images.Object);
        Visit visit = Visit.Create(
            VisitId.Parse("visit-1"),
            "owner-1",
            park.Id,
            VisitDate.ForYear(1990),
            null,
            LocalServiceDayConvention.VisitStartLocalDate,
            null,
            null,
            new DateTime(2026, 9, 27, 8, 0, 0, DateTimeKind.Utc));

        PassportHistoricalTargetContext context = await resolver.ResolveAsync(
            visit,
            new[] { item.Id },
            CancellationToken.None);

        Assert.Empty(context.Targets);
        Assert.Contains(item.Id, context.CanonicallyExcludedParkItemIds);
        parks.VerifyAll();
        parkItems.VerifyAll();
        facts.VerifyAll();
        snapshots.VerifyAll();
        currentTargets.VerifyAll();
        zones.VerifyNoOtherCalls();
        images.VerifyNoOtherCalls();
    }

    private static HistoricalParkRolloutGateAssessmentService CreateRolloutGateAssessmentService()
    {
        return new HistoricalParkRolloutGateAssessmentService(
            new ParkHistoricalSnapshotBuilder(),
            new HistoricalParkRolloutGateEvaluator(),
            PublicParkHistoryTestData.CreatePublicSourceRepository(
                Array.Empty<HistoricalFact>()));
    }

    private static ParkHistoricalSnapshot CreateNonAttractionSnapshot(
        string parkId,
        HistoricalInstant instant,
        HistoricalSubject subject,
        ParkItemCategory historicalCategory)
    {
        HistoricalSubjectSnapshot snapshot = new HistoricalSubjectSnapshot(
            subject,
            HistoricalOperationalState.Unknown,
            HistoricalPresenceExtent.None,
            Array.Empty<HistoricalPresenceInterval>(),
            new[]
            {
                new HistoricalAttributeSnapshot(
                    HistoricalAttributeKind.Category,
                    HistoricalAttributeValueState.Known,
                    historicalCategory.ToString(),
                    new[] { historicalCategory.ToString() },
                    Array.Empty<HistoricalSnapshotReason>(),
                    Array.Empty<Guid>()),
            },
            Array.Empty<HistoricalSnapshotReason>(),
            Array.Empty<Guid>());
        HistoricalCoverage coverage = new HistoricalCoverage(
            1,
            0,
            0,
            1,
            new HistoricalFieldCoverage(0, 1),
            new HistoricalFieldCoverage(0, 1),
            null,
            HistoricalCoverageStatus.Partial);
        return new ParkHistoricalSnapshot(
            parkId,
            instant,
            new[] { snapshot },
            coverage,
            Array.Empty<HistoricalAmbiguity>(),
            ParkHistoricalSnapshotBuilder.CurrentMethodologyVersion);
    }
}
