using System.Text.Json;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Core.Domain.Sharing;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Sharing;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class VisitRecapShareSnapshotMongoMapperTests
{
    [Fact]
    public void Mapping_ShouldRoundTripTheExactApprovedPublicSnapshot()
    {
        VisitRecapShareSnapshot snapshot = new VisitRecapShareSnapshot(
            SharePublicationId.Parse("publication-1"),
            3,
            7,
            12,
            1,
            ShareDatePrecision.Month,
            new[] { ShareContentField.RideCount, ShareContentField.PublicCaption },
            "fingerprint-a",
            new VisitRecapSharePreviewResult(
                "park-1",
                "Denain Évasion",
                new VisitRecapShareDateResult(
                    2026,
                    7,
                    null,
                    ShareDatePrecision.Month,
                    false),
                1,
                2,
                new[] { "Attraction" },
                null,
                null,
                new VisitRecapShareHighlightResult("Le Galion", 2, null),
                new[]
                {
                    new VisitRecapShareItemResult(
                        "item-1",
                        "Le Galion",
                        "Attraction",
                        2,
                        null,
                        false),
                },
                "Un beau souvenir",
                false,
                false,
                false),
            new DateTime(2026, 9, 7, 0, 0, 0, DateTimeKind.Utc));

        VisitRecapShareSnapshotDocument document = snapshot.ToDocument();
        VisitRecapShareSnapshot restored = document.ToDomain();

        Assert.Equal("publication-1:3", document.Id);
        Assert.Equal(snapshot.PublicationId, restored.PublicationId);
        Assert.Equal(snapshot.PublicationVersion, restored.PublicationVersion);
        Assert.Equal(snapshot.PublicationStateVersion, restored.PublicationStateVersion);
        Assert.Equal(snapshot.SourceVersion, restored.SourceVersion);
        Assert.Equal(snapshot.ContentFingerprint, restored.ContentFingerprint);
        Assert.Equal("Denain Évasion", restored.Content.ParkName);
        Assert.Equal("Le Galion", Assert.Single(restored.Content.Items).Name);
        string serialized = JsonSerializer.Serialize(document);
        Assert.DoesNotContain("privateNote", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("privateComment", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ownerUserId", serialized, StringComparison.OrdinalIgnoreCase);
    }
}
