using System.Text.Json;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Core.Domain.Sharing;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Sharing;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Bson;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class YearRecapShareSnapshotMongoMapperTests
{
    [Fact]
    public void Mapping_ShouldRoundTripOnlyTheApprovedPublicAnnualRecap()
    {
        YearRecapShareSnapshot snapshot = new YearRecapShareSnapshot(
            SharePublicationId.Parse("publication-1"),
            2,
            5,
            12,
            1,
            ShareDatePrecision.Year,
            new[] { ShareContentField.RideCount, ShareContentField.PublicCaption },
            "fingerprint-a",
            new YearRecapSharePreviewResult(
                2026,
                null,
                2,
                1,
                0.5,
                4,
                2,
                null,
                new[] { "Attraction" },
                null,
                null,
                Array.Empty<YearRecapShareParkResult>(),
                new YearRecapShareHighlightResult("Le Galion", 3, 2, 4.5, false),
                null,
                null,
                Array.Empty<YearRecapShareHighlightResult>(),
                "Une belle année",
                false,
                "passport-year-recap-v1",
                false),
            new DateTime(2026, 9, 11, 0, 0, 0, DateTimeKind.Utc));

        YearRecapShareSnapshotDocument document = snapshot.ToDocument();
        YearRecapShareSnapshot restored = document.ToDomain();
        BsonDocument bson = document.ToBsonDocument();

        Assert.Equal("publication-1:2", document.Id);
        Assert.Equal(snapshot.Content.Year, restored.Content.Year);
        Assert.Equal(snapshot.Content.VisitCount, restored.Content.VisitCount);
        Assert.Equal(snapshot.Content.Categories, restored.Content.Categories);
        Assert.Equal(snapshot.Content.MostRepeatedItem, restored.Content.MostRepeatedItem);
        Assert.Equal(snapshot.Content.PublicCaption, restored.Content.PublicCaption);
        Assert.True(bson["content"].AsBsonDocument.Contains("visitCount"));
        Assert.False(bson["content"].AsBsonDocument.Contains("parkCount"));
        string serialized = JsonSerializer.Serialize(document);
        Assert.DoesNotContain("privateNote", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("privateComment", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ownerUserId", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("parkId", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("parkItemId", serialized, StringComparison.OrdinalIgnoreCase);
    }
}
