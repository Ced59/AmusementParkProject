using System.Text.Json;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Core.Domain.Sharing;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Sharing;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Bson;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class PassportProfileShareSnapshotMongoMapperTests
{
    [Fact]
    public void Mapping_ShouldRoundTripPublicContentWithoutTechnicalIdentifiers()
    {
        PassportProfileSharePreviewResult content = new PassportProfileSharePreviewResult(
            "Alex",
            null,
            "Mon histoire de parcs",
            ShareVisibility.Unlisted,
            true,
            1,
            2,
            6,
            3,
            new PassportProfileShareRatingSummaryResult(2, 2, 4.5),
            new PassportProfileShareRatingSummaryResult(4, 6, 4.25),
            new[] { new PassportProfileShareCountryResult("FR", 1, 2) },
            new[] { new PassportProfileShareYearResult(2026, 2, 1, 6) },
            new[]
            {
                new PassportProfileShareParkResult(
                    "Denain Évasion",
                    "FR",
                    2,
                    2025,
                    2026,
                    6,
                    new PassportProfileShareRatingSummaryResult(2, 2, 4.5)),
            },
            new[] { new PassportProfileShareRatingResult("Park", "Denain Évasion", null, null, 5) },
            new[] { new PassportProfileShareMissedItemResult("La Maison d’Houdini", "MissedClosure", 1) },
            false,
            "passport-profile-v1",
            false);
        PassportProfileShareSnapshot snapshot = new PassportProfileShareSnapshot(
            SharePublicationId.Parse("publication-1"),
            3,
            8,
            12,
            1,
            ShareDatePrecision.Year,
            new[] { ShareContentField.RideCount, ShareContentField.GeographicStatistics },
            "fingerprint-a",
            new PassportProfileShareInput(
                new[] { 2026 },
                new[] { "private-park-id" },
                Array.Empty<string>(),
                "Mon histoire de parcs",
                ShareVisibility.Unlisted,
                true),
            content,
            new DateTime(2026, 9, 12, 0, 0, 0, DateTimeKind.Utc));

        PassportProfileShareSnapshotDocument document = snapshot.ToDocument();
        PassportProfileShareSnapshot restored = document.ToDomain();
        BsonDocument publicContent = document.Content.ToBsonDocument();
        string serializedPublicContent = JsonSerializer.Serialize(document.Content);

        Assert.Equal(content.DisplayName, restored.Content.DisplayName);
        Assert.Equal(content.Visibility, restored.Content.Visibility);
        Assert.Equal(content.VisitCount, restored.Content.VisitCount);
        Assert.Equal(content.Countries, restored.Content.Countries);
        Assert.Equal(content.Years, restored.Content.Years);
        Assert.Equal(content.Parks, restored.Content.Parks);
        Assert.Equal(content.PersonalRanking, restored.Content.PersonalRanking);
        Assert.Equal(content.MissedItems, restored.Content.MissedItems);
        Assert.Equal(snapshot.Selection.SelectedYears, restored.Selection.SelectedYears);
        Assert.Equal(snapshot.Selection.SelectedParkIds, restored.Selection.SelectedParkIds);
        Assert.Equal(snapshot.Selection.Visibility, restored.Selection.Visibility);
        Assert.True(publicContent.Contains("parks"));
        Assert.DoesNotContain("private-park-id", serializedPublicContent, StringComparison.Ordinal);
        Assert.DoesNotContain("parkId", serializedPublicContent, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ownerUserId", serializedPublicContent, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("visitId", serializedPublicContent, StringComparison.OrdinalIgnoreCase);
    }
}
