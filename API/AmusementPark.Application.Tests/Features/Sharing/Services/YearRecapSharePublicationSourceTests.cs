using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Application.Features.Sharing.Services;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Sharing.Services;

public sealed class YearRecapSharePublicationSourceTests
{
    [Fact]
    public async Task GetOwnedSourceVersionAsync_ShouldReconcileFingerprintIntoAMonotonicRevision()
    {
        YearRecapSourceData source = new YearRecapSourceData(
            Array.Empty<AmusementPark.Core.Domain.Visits.PassportVisitStatisticsObservation>(),
            Array.Empty<AmusementPark.Core.Domain.Visits.PassportRideStatisticsObservation>(),
            new Dictionary<string, string?>(),
            "SOURCE-FINGERPRINT",
            true);
        Mock<IYearRecapSourceReader> reader = new Mock<IYearRecapSourceReader>(MockBehavior.Strict);
        reader.Setup(value => value.ReadOwnedCompletedYearAsync(
                "owner-1",
                2026,
                CancellationToken.None))
            .ReturnsAsync(source);
        Mock<IShareSourceRevisionRepository> revisions =
            new Mock<IShareSourceRevisionRepository>(MockBehavior.Strict);
        revisions.Setup(value => value.GetSnapshotAsync(
                It.Is<IReadOnlyCollection<string>>(scopes =>
                    scopes.SequenceEqual(new[] { PersonalRankingShareSourceScope.PublicCatalog })),
                CancellationToken.None))
            .ReturnsAsync(new Dictionary<string, ShareSourceRevision>(StringComparer.Ordinal)
            {
                [PersonalRankingShareSourceScope.PublicCatalog] =
                    new ShareSourceRevision(4, 0, DateTime.UtcNow),
            });
        revisions.Setup(value => value.ReconcileFingerprintAsync(
                YearRecapShareSourceScope.Create("owner-1", 2026),
                "SOURCE-FINGERPRINT",
                CancellationToken.None))
            .ReturnsAsync(new ShareSourceRevision(7, 0, DateTime.UtcNow));
        YearRecapSharePublicationSource publicationSource =
            new YearRecapSharePublicationSource(reader.Object, revisions.Object);

        ApplicationResult<YearRecapShareSourceRevision> result =
            await publicationSource.GetOwnedSourceVersionAsync(
                "owner-1",
                2026,
                CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(11, result.Value!.Version);
        Assert.Equal("SOURCE-FINGERPRINT", result.Value.SourceFingerprint);
        reader.VerifyAll();
        revisions.VerifyAll();
    }
}
