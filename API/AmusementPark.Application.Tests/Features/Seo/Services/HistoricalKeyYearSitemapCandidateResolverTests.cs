using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.History.Handlers;
using AmusementPark.Application.Tests.Features.History.Handlers;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Services;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Parks;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Seo.Services;

public sealed class HistoricalKeyYearSitemapCandidateResolverTests
{
    [Fact]
    public void Resolve_WithMajorDocumentedYear_AddsOnlyCanonicalYearCandidate()
    {
        Park park = PublicParkHistoryTestData.CreatePark();
        ParkItem item = PublicParkHistoryTestData.CreateParkItem("item-1", "Attraction témoin");
        HistoricalSubject parkSubject = new HistoricalSubject(
            HistoricalSubjectType.Park,
            park.Id,
            park.Name!,
            HistoricalSubjectPublicationPolicy.FollowCurrentSubject,
            park.Id);
        HistoricalSubject itemSubject = new HistoricalSubject(
            HistoricalSubjectType.ParkItem,
            item.Id,
            item.Name!,
            HistoricalSubjectPublicationPolicy.FollowCurrentSubject,
            park.Id);
        HistoricalFact parkOpening = PublicParkHistoryTestData.CreateOpeningFact(parkSubject, 1998);
        HistoricalFact itemOpening = PublicParkHistoryTestData.CreateOpeningFact(itemSubject, 1990);
        Mock<IParkHistoricalSnapshotBuilder> snapshotBuilder = new Mock<IParkHistoricalSnapshotBuilder>();
        snapshotBuilder
            .Setup(builder => builder.Build(
                park.Id,
                It.IsAny<HistoricalInstant>(),
                It.IsAny<IReadOnlyCollection<HistoricalSubject>>(),
                It.IsAny<IReadOnlyCollection<HistoricalFact>>()))
            .Returns((
                string parkId,
                HistoricalInstant instant,
                IReadOnlyCollection<HistoricalSubject> subjects,
                IReadOnlyCollection<HistoricalFact> facts) => CreateEligibleSnapshot(
                    parkId,
                    instant,
                    parkSubject,
                    parkOpening.Id,
                    itemOpening.Id));

        IReadOnlyCollection<HistoricalKeyYearSitemapCandidate> result =
            HistoricalKeyYearSitemapCandidateResolver.Resolve(
                new[] { park },
                new[] { item },
                Array.Empty<ParkZone>(),
                new[] { parkOpening, itemOpening },
                snapshotBuilder.Object);

        Assert.Contains(result, candidate => candidate.ParkId == park.Id && candidate.Year == 1998);
        Assert.All(result, candidate => Assert.Contains(candidate.Year, new[] { 1990, 1998 }));
    }

    [Fact]
    public void Resolve_WithHiddenCurrentSubject_DoesNotUseItsFact()
    {
        Park park = PublicParkHistoryTestData.CreatePark();
        ParkItem hiddenItem = PublicParkHistoryTestData.CreateParkItem("hidden-item", "Attraction masquée", false);
        HistoricalSubject hiddenSubject = new HistoricalSubject(
            HistoricalSubjectType.ParkItem,
            hiddenItem.Id,
            hiddenItem.Name!,
            HistoricalSubjectPublicationPolicy.FollowCurrentSubject,
            park.Id);
        HistoricalFact hiddenFact = PublicParkHistoryTestData.CreateOpeningFact(hiddenSubject, 1998);
        Mock<IParkHistoricalSnapshotBuilder> snapshotBuilder = new Mock<IParkHistoricalSnapshotBuilder>(MockBehavior.Strict);

        IReadOnlyCollection<HistoricalKeyYearSitemapCandidate> result =
            HistoricalKeyYearSitemapCandidateResolver.Resolve(
                new[] { park },
                new[] { hiddenItem },
                Array.Empty<ParkZone>(),
                new[] { hiddenFact },
                snapshotBuilder.Object);

        Assert.Empty(result);
    }

    [Fact]
    public void Resolve_WithSubjectMovedToAnotherPark_DoesNotExposeItsFormerParkFact()
    {
        Park formerPark = PublicParkHistoryTestData.CreatePark();
        formerPark.Id = "park-former";
        formerPark.Name = "Ancien parc";
        Park currentPark = PublicParkHistoryTestData.CreatePark();
        currentPark.Id = "park-current";
        currentPark.Name = "Parc actuel";
        ParkItem movedItem = PublicParkHistoryTestData.CreateParkItem("moved-item", "Attraction déplacée");
        movedItem.ParkId = currentPark.Id;
        HistoricalSubject formerSubject = new HistoricalSubject(
            HistoricalSubjectType.ParkItem,
            movedItem.Id,
            movedItem.Name!,
            HistoricalSubjectPublicationPolicy.FollowCurrentSubject,
            formerPark.Id);
        HistoricalFact formerFact = PublicParkHistoryTestData.CreateOpeningFact(formerSubject, 1998);
        Mock<IParkHistoricalSnapshotBuilder> snapshotBuilder =
            new Mock<IParkHistoricalSnapshotBuilder>(MockBehavior.Strict);

        IReadOnlyCollection<HistoricalKeyYearSitemapCandidate> result =
            HistoricalKeyYearSitemapCandidateResolver.Resolve(
                new[] { formerPark, currentPark },
                new[] { movedItem },
                Array.Empty<ParkZone>(),
                new[] { formerFact },
                snapshotBuilder.Object);

        Assert.Empty(result);
        snapshotBuilder.VerifyNoOtherCalls();
    }

    private static ParkHistoricalSnapshot CreateEligibleSnapshot(
        string parkId,
        HistoricalInstant instant,
        HistoricalSubject subject,
        params Guid[] supportingFactIds)
    {
        HistoricalSubjectSnapshot subjectSnapshot = new HistoricalSubjectSnapshot(
            subject,
            HistoricalOperationalState.Unknown,
            HistoricalPresenceExtent.None,
            Array.Empty<HistoricalPresenceInterval>(),
            Array.Empty<HistoricalAttributeSnapshot>(),
            Array.Empty<HistoricalSnapshotReason>(),
            supportingFactIds);
        HistoricalCoverage coverage = new HistoricalCoverage(
            1,
            1,
            0,
            0,
            new HistoricalFieldCoverage(1, 1),
            new HistoricalFieldCoverage(0, 0),
            DateTime.UtcNow,
            HistoricalCoverageStatus.Substantial);
        return new ParkHistoricalSnapshot(
            parkId,
            instant,
            new[] { subjectSnapshot },
            coverage,
            Array.Empty<HistoricalAmbiguity>(),
            ParkHistoricalSnapshotBuilder.CurrentMethodologyVersion);
    }
}
