using AmusementPark.Application.Features.History.Handlers;
using AmusementPark.Application.Features.History.Ports;
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
    public async Task ResolveRecordedAsync_ShouldKeepHiddenExistingTargetWithoutExposingItForNewEntries()
    {
        const string ParkId = "park-1";
        const string ParkItemId = "hidden-ride";
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
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { ParkItemId })),
                CancellationToken.None))
            .ReturnsAsync(new Dictionary<string, VisitTarget>(StringComparer.Ordinal)
            {
                [ParkItemId] = new VisitTarget(
                    ParkItemId,
                    ParkId,
                    "Attraction masquée",
                    ParkItemCategory.Attraction,
                    null,
                    null,
                    null,
                    false),
            });
        Mock<IImageRepository> images = new Mock<IImageRepository>(MockBehavior.Strict);
        PassportHistoricalTargetResolver resolver = new PassportHistoricalTargetResolver(
            new PublicParkHistoricalDataLoader(
                parks.Object,
                parkItems.Object,
                zones.Object,
                facts.Object),
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

        PassportHistoricalTarget target = Assert.Single(recorded.Targets).Value;
        Assert.Equal("Attraction masquée", target.Name);
        Assert.True(target.IsHistoricalOnly);
        Assert.True(target.IsValidationFallback);
        Assert.Empty(newEntry.Targets);
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
        parkItems.Setup(repository => repository.GetByParkIdAsync(
                park.Id,
                false,
                CancellationToken.None))
            .ReturnsAsync(new[] { item });
        zones.Setup(repository => repository.GetByParkIdAsync(
                park.Id,
                CancellationToken.None))
            .ReturnsAsync(Array.Empty<ParkZone>());
        facts.Setup(repository => repository.GetLatestDecisionEligibleRevisionsForParkAsync(
                park.Id,
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
                facts.Object),
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
        Assert.Null(target.MainImageId);
        PassportHistoricalTarget laterTarget = Assert.Single(
            contexts[VisitDate.ForYear(2005)].Targets).Value;
        Assert.Equal(HistoricalOperationalState.KnownOpen, laterTarget.OperationalState);
        parks.VerifyAll();
        parkItems.VerifyAll();
        zones.VerifyAll();
        facts.VerifyAll();
        currentTargets.VerifyAll();
        parks.Verify(repository => repository.GetByIdAsync(
            park.Id,
            false,
            CancellationToken.None), Times.Once);
        currentTargets.Verify(resolver => resolver.ResolveAsync(
            It.IsAny<IReadOnlyCollection<string>>(),
            CancellationToken.None), Times.Once);
        images.VerifyNoOtherCalls();
    }
}
