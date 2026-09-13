using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Core.Domain.Sharing;
using AmusementPark.Infrastructure.Services.Passport;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Services.Passport;

public sealed class PassportShareSnapshotExportSelectorTests
{
    private static readonly DateTime NowUtc =
        new DateTime(2026, 9, 14, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void SelectLatestRetained_PreservesApprovedSnapshotsAfterLifecycleChanges()
    {
        ShareContentPolicy policy = ShareContentPolicy.Create(
            SharePublicationType.VisitRecap,
            ShareDatePrecision.Day,
            new[] { ShareContentField.PublicCaption });
        SharePublication revoked = CreatePublishedPublication(
            "publication-revoked",
            "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8",
            policy);
        VisitRecapShareSnapshot revokedSnapshot = CreateSnapshot(
            revoked,
            policy,
            "Revoked caption");
        revoked.Revoke(revoked.PublicationVersion, NowUtc.AddMinutes(2));
        SharePublication needsReview = CreatePublishedPublication(
            "publication-needs-review",
            "AQECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8",
            policy);
        VisitRecapShareSnapshot needsReviewSnapshot = CreateSnapshot(
            needsReview,
            policy,
            "Needs review caption");
        needsReview.MarkSourceChanged(2, NowUtc.AddMinutes(2));
        SharePublication rotated = CreatePublishedPublication(
            "publication-rotated",
            "AgECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8",
            policy);
        VisitRecapShareSnapshot rotatedPreviousSnapshot = CreateSnapshot(
            rotated,
            policy,
            "Previous caption");
        rotated.RotateToken(
            ShareToken.Parse("AwECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8"),
            rotated.PublicationVersion,
            NowUtc.AddMinutes(2));
        VisitRecapShareSnapshot rotatedCurrentSnapshot = CreateSnapshot(
            rotated,
            policy,
            "Current caption");
        IReadOnlyDictionary<string, long> versionUpperBounds =
            new Dictionary<string, long>(StringComparer.Ordinal)
            {
                [revoked.Id.Value] = revoked.PublicationVersion,
                [needsReview.Id.Value] = needsReview.PublicationVersion,
                [rotated.Id.Value] = rotated.PublicationVersion,
            };
        VisitRecapShareSnapshot futureSnapshot = rotatedCurrentSnapshot with
        {
            PublicationVersion = rotated.PublicationVersion + 1,
            Content = rotatedCurrentSnapshot.Content with { PublicCaption = "Future caption" },
        };

        IReadOnlyCollection<VisitRecapShareSnapshot> result =
            PassportShareSnapshotExportSelector.SelectLatestRetained(
                new[]
                {
                    revokedSnapshot,
                    needsReviewSnapshot,
                    rotatedPreviousSnapshot,
                    rotatedCurrentSnapshot,
                    futureSnapshot,
                },
                versionUpperBounds,
                static snapshot => snapshot.PublicationId.Value,
                static snapshot => snapshot.PublicationVersion,
                static snapshot => snapshot.CreatedAtUtc);

        Assert.Equal(3, result.Count);
        Assert.Contains(result, snapshot =>
            snapshot.PublicationId == revoked.Id
            && snapshot.Content.PublicCaption == "Revoked caption");
        Assert.Contains(result, snapshot =>
            snapshot.PublicationId == needsReview.Id
            && snapshot.Content.PublicCaption == "Needs review caption");
        VisitRecapShareSnapshot selectedRotated = Assert.Single(
            result,
            snapshot => snapshot.PublicationId == rotated.Id);
        Assert.Equal(rotated.PublicationVersion, selectedRotated.PublicationVersion);
        Assert.Equal("Current caption", selectedRotated.Content.PublicCaption);
    }

    private static SharePublication CreatePublishedPublication(
        string id,
        string token,
        ShareContentPolicy policy)
    {
        SharePublication publication = SharePublication.Create(
            SharePublicationId.Parse(id),
            "user-1",
            SharePublicationType.VisitRecap,
            string.Concat("visit:user-1:", id),
            policy,
            1,
            NowUtc);
        publication.Publish(
            ShareToken.Parse(token),
            ShareVisibility.Unlisted,
            1,
            policy,
            0,
            NowUtc.AddMinutes(1));
        return publication;
    }

    private static VisitRecapShareSnapshot CreateSnapshot(
        SharePublication publication,
        ShareContentPolicy policy,
        string publicCaption)
    {
        return new VisitRecapShareSnapshot(
            publication.Id,
            publication.PublicationVersion,
            publication.Version,
            publication.SourceVersion,
            policy.SchemaVersion,
            policy.DatePrecision,
            policy.IncludedFields,
            "content-fingerprint",
            new VisitRecapSharePreviewResult(
                "park-internal",
                "Europa Park",
                null,
                null,
                null,
                Array.Empty<string>(),
                null,
                null,
                null,
                Array.Empty<VisitRecapShareItemResult>(),
                publicCaption,
                true,
                false,
                false),
            NowUtc.AddMinutes(publication.PublicationVersion));
    }
}
