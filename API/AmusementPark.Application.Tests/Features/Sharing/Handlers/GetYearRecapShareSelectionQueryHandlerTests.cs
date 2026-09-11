using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Handlers;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Application.Features.Sharing.Queries;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Application.Features.Sharing.Services;
using AmusementPark.Core.Domain.Sharing;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Sharing.Handlers;

public sealed class GetYearRecapShareSelectionQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldRestoreTheCaptionFromARevokedSnapshot()
    {
        DateTime nowUtc = new DateTime(2026, 9, 11, 5, 0, 0, DateTimeKind.Utc);
        SharePublicationId publicationId = SharePublicationId.Parse("publication-1");
        string scope = YearRecapShareSourceScope.Create("owner-1", 2026);
        ShareContentPolicy policy = ShareContentPolicy.Create(
            SharePublicationType.YearRecap,
            ShareDatePrecision.Year,
            new[] { ShareContentField.PublicCaption });
        SharePublication publication = SharePublication.Restore(
            publicationId,
            "owner-1",
            SharePublicationType.YearRecap,
            scope,
            null,
            SharePublicationStatus.Revoked,
            ShareVisibility.Private,
            policy,
            7,
            1,
            2,
            nowUtc.AddMinutes(-5),
            nowUtc,
            nowUtc.AddHours(-1),
            nowUtc,
            "fingerprint");
        YearRecapShareSnapshot snapshot = new YearRecapShareSnapshot(
            publicationId,
            1,
            1,
            7,
            policy.SchemaVersion,
            policy.DatePrecision,
            policy.IncludedFields,
            "fingerprint",
            new YearRecapSharePreviewResult(
                2026, null, 1, 0, 0, null, null, null,
                Array.Empty<string>(), null, null,
                Array.Empty<YearRecapShareParkResult>(), null, null, null,
                Array.Empty<YearRecapShareHighlightResult>(),
                "Souvenir déjà public", false, "passport-year-recap-v1", false),
            nowUtc);
        Mock<ISharePublicationRepository> publications =
            new Mock<ISharePublicationRepository>(MockBehavior.Strict);
        publications.Setup(value => value.GetOwnedBySourceAsync(
                "owner-1",
                SharePublicationType.YearRecap,
                scope,
                CancellationToken.None))
            .ReturnsAsync(publication);
        Mock<IYearRecapShareSnapshotRepository> snapshots =
            new Mock<IYearRecapShareSnapshotRepository>(MockBehavior.Strict);
        snapshots.Setup(value => value.GetAsync(
                publicationId,
                1,
                CancellationToken.None))
            .ReturnsAsync(snapshot);
        GetYearRecapShareSelectionQueryHandler handler =
            new GetYearRecapShareSelectionQueryHandler(
                publications.Object,
                snapshots.Object);

        ApplicationResult<YearRecapShareSelectionResult> result = await handler.HandleAsync(
            new GetYearRecapShareSelectionQuery("owner-1", 2026),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.HasSavedSnapshot);
        Assert.Equal("Souvenir déjà public", result.Value.SavedPublicCaption);
        publications.VerifyAll();
        snapshots.VerifyAll();
    }
}
