using System.Text.Json;
using AmusementPark.Application.Features.ParkGraphUpserts.Results;
using AmusementPark.Application.Features.ParkGraphUpserts.Services;
using AmusementPark.Core.Domain.Parks;
using Xunit;

namespace AmusementPark.Application.Tests.Features.ParkGraphUpserts.Services;

public sealed class ParkGraphOfficialMapUpsertPatcherTests
{
    [Fact]
    public void Patch_WhenOneEntryIsInvalid_ShouldNotApplyAnyOfficialMapFromTheBatch()
    {
        Park park = new Park
        {
            Id = "park-1",
            Name = "Map Park",
            OfficialMaps = new List<ParkOfficialMap>(),
        };
        ParkGraphUpsertResult result = new ParkGraphUpsertResult();
        using JsonDocument document = JsonDocument.Parse("""
        {
          "officialMaps": [
            {
              "id": "map-2025",
              "year": 2025,
              "format": "Pdf",
              "documentUrl": "https://park.example/map-2025.pdf"
            },
            {
              "id": "map-invalid",
              "year": 1799,
              "format": "Pdf",
              "documentUrl": "https://park.example/map-old.pdf"
            }
          ]
        }
        """);

        ParkGraphOfficialMapUpsertPatcher.Patch(park, document.RootElement, result);

        Assert.NotEmpty(result.Errors);
        Assert.Empty(park.OfficialMaps);
    }
}
