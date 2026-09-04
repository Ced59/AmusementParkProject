using System.IO.Compression;
using System.Text;
using System.Text.Json;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Services;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Core.Domain.Visits;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Passport.Services;

public sealed class CanonicalVisitExportWriterTests
{
    private static readonly DateTime NowUtc =
        new DateTime(2026, 9, 4, 10, 30, 0, DateTimeKind.Utc);

    [Fact]
    public void Write_JsonContainsCompleteVersionedSectionsAndComfortNames()
    {
        CanonicalVisitExportWriter writer = new CanonicalVisitExportWriter();
        PassportExportWriteRequest request = CreateRequest(PassportExportFormat.Json);

        PassportExportArtifact artifact = writer.Write(request);

        using JsonDocument document = JsonDocument.Parse(artifact.Content);
        JsonElement root = document.RootElement;
        Assert.Equal(CanonicalVisitExportWriter.SchemaVersion, root.GetProperty("schema").GetProperty("version").GetInt32());
        Assert.False(root.GetProperty("schema").TryGetProperty("exportId", out JsonElement _));
        Assert.Equal("park-0001", root.GetProperty("parks")[0].GetProperty("reference").GetString());
        Assert.Equal("Europa Park", root.GetProperty("parks")[0].GetProperty("name").GetString());
        Assert.Equal("park-item-0001", root.GetProperty("parkItems")[0].GetProperty("reference").GetString());
        Assert.Equal("park-0001", root.GetProperty("parkItems")[0].GetProperty("parkReference").GetString());
        Assert.Equal("visit-0001", root.GetProperty("visits")[0].GetProperty("reference").GetString());
        Assert.Equal("park-0001", root.GetProperty("visits")[0].GetProperty("parkReference").GetString());
        Assert.Equal("Europa Park", root.GetProperty("visits")[0].GetProperty("parkName").GetString());
        Assert.Equal(
            "occurrence-0001",
            root.GetProperty("rideOccurrences")[0].GetProperty("reference").GetString());
        Assert.Equal(
            "visit-0001",
            root.GetProperty("rideOccurrences")[0].GetProperty("visitReference").GetString());
        Assert.Equal(
            "park-item-0001",
            root.GetProperty("rideOccurrences")[0].GetProperty("parkItemReference").GetString());
        Assert.Equal("Silver Star", root.GetProperty("rideOccurrences")[0].GetProperty("parkItemName").GetString());
        Assert.Equal(8, root.GetProperty("visitAssessments")[0].GetProperty("valueHalfSteps").GetInt32());
        Assert.Equal(9, root.GetProperty("rideAssessments")[0].GetProperty("valueHalfSteps").GetInt32());
        string content = Encoding.UTF8.GetString(artifact.Content);
        Assert.DoesNotContain("0123456789abcdef0123456789abcdef", content, StringComparison.Ordinal);
        Assert.DoesNotContain("01JTESTVISIT00000000000000", content, StringComparison.Ordinal);
        Assert.DoesNotContain("01JTESTOCCURRENCE0000000000", content, StringComparison.Ordinal);
        Assert.DoesNotContain("park-1", content, StringComparison.Ordinal);
        Assert.DoesNotContain("item-1", content, StringComparison.Ordinal);
        Assert.EndsWith(".json", artifact.FileName, StringComparison.Ordinal);
        Assert.DoesNotContain("01234567", artifact.FileName, StringComparison.Ordinal);
        Assert.Equal(64, artifact.ChecksumSha256.Length);
    }

    [Fact]
    public void Write_CsvCreatesFourIndependentTablesAndSchemaMetadata()
    {
        CanonicalVisitExportWriter writer = new CanonicalVisitExportWriter();
        PassportExportWriteRequest request = CreateRequest(PassportExportFormat.Csv);

        PassportExportArtifact artifact = writer.Write(request);

        using MemoryStream stream = new MemoryStream(artifact.Content);
        using ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read);
        string[] names = archive.Entries.Select(static entry => entry.FullName).Order().ToArray();
        Assert.Equal(
            new[]
            {
                "park-items.csv",
                "parks.csv",
                "ride-assessments.csv",
                "ride-occurrences.csv",
                "schema.json",
                "visit-assessments.csv",
                "visits.csv",
            },
            names);
        ZipArchiveEntry visitsEntry = Assert.Single(archive.Entries, static entry => entry.FullName == "visits.csv");
        using StreamReader reader = new StreamReader(visitsEntry.Open(), Encoding.UTF8);
        string visitsCsv = reader.ReadToEnd();
        Assert.Contains("\"Souvenir, privé\"", visitsCsv, StringComparison.Ordinal);
        Assert.Contains("'=1+1", visitsCsv, StringComparison.Ordinal);
        ZipArchiveEntry schemaEntry = Assert.Single(
            archive.Entries,
            static entry => entry.FullName == "schema.json");
        using JsonDocument schema = JsonDocument.Parse(schemaEntry.Open());
        Assert.Equal(
            "leading-apostrophe-for-=+-@-cells",
            schema.RootElement.GetProperty("formulaNeutralization").GetString());
        Assert.False(schema.RootElement.GetProperty("schema").TryGetProperty("exportId", out JsonElement _));
        StringBuilder exportedText = new StringBuilder();
        foreach (ZipArchiveEntry entry in archive.Entries)
        {
            using StreamReader entryReader = new StreamReader(entry.Open(), Encoding.UTF8);
            exportedText.Append(entryReader.ReadToEnd());
        }

        string content = exportedText.ToString();
        Assert.Contains("visit-0001", content, StringComparison.Ordinal);
        Assert.Contains("occurrence-0001", content, StringComparison.Ordinal);
        Assert.DoesNotContain("0123456789abcdef0123456789abcdef", content, StringComparison.Ordinal);
        Assert.DoesNotContain("01JTESTVISIT00000000000000", content, StringComparison.Ordinal);
        Assert.DoesNotContain("01JTESTOCCURRENCE0000000000", content, StringComparison.Ordinal);
        Assert.DoesNotContain("park-1", content, StringComparison.Ordinal);
        Assert.DoesNotContain("item-1", content, StringComparison.Ordinal);
        Assert.EndsWith(".zip", artifact.FileName, StringComparison.Ordinal);
        Assert.DoesNotContain("01234567", artifact.FileName, StringComparison.Ordinal);
    }

    [Fact]
    public void Write_WhenCatalogDataIsUnavailable_ShouldNotFallBackToInternalIdentifiers()
    {
        CanonicalVisitExportWriter writer = new CanonicalVisitExportWriter();
        PassportExportWriteRequest source = CreateRequest(PassportExportFormat.Json);
        PassportExportWriteRequest request = source with
        {
            Parks = new Dictionary<string, Park>(StringComparer.Ordinal),
            ParkItems = new Dictionary<string, VisitTarget>(StringComparer.Ordinal),
        };

        PassportExportArtifact artifact = writer.Write(request);

        string content = Encoding.UTF8.GetString(artifact.Content);
        Assert.Contains("Unavailable park", content, StringComparison.Ordinal);
        Assert.Contains("Unavailable attraction", content, StringComparison.Ordinal);
        Assert.DoesNotContain("park-1", content, StringComparison.Ordinal);
        Assert.DoesNotContain("item-1", content, StringComparison.Ordinal);
    }

    [Fact]
    public void Write_WhenCatalogTargetMovedToAnotherPark_ShouldNotRewriteHistoricalParentage()
    {
        CanonicalVisitExportWriter writer = new CanonicalVisitExportWriter();
        PassportExportWriteRequest source = CreateRequest(PassportExportFormat.Json);
        VisitTarget movedTarget = source.ParkItems["item-1"] with { ParkId = "park-2" };
        PassportExportWriteRequest request = source with
        {
            ParkItems = new Dictionary<string, VisitTarget>(StringComparer.Ordinal)
            {
                [movedTarget.ParkItemId] = movedTarget,
            },
        };

        PassportExportArtifact artifact = writer.Write(request);

        using JsonDocument document = JsonDocument.Parse(artifact.Content);
        JsonElement root = document.RootElement;
        Assert.Equal(
            "park-0001",
            root.GetProperty("parkItems")[0].GetProperty("parkReference").GetString());
        Assert.Equal(
            "Unavailable attraction",
            root.GetProperty("parkItems")[0].GetProperty("name").GetString());
        Assert.Equal(
            "Unavailable attraction",
            root.GetProperty("rideOccurrences")[0].GetProperty("parkItemName").GetString());
        Assert.DoesNotContain("Silver Star", Encoding.UTF8.GetString(artifact.Content), StringComparison.Ordinal);
    }

    [Fact]
    public void Write_WhenTwoExportsCompleteInTheSameSecond_ShouldKeepUniqueReadableFileNames()
    {
        CanonicalVisitExportWriter writer = new CanonicalVisitExportWriter();
        PassportExportWriteRequest firstRequest = CreateRequest(PassportExportFormat.Json);
        PassportExportWriteRequest secondRequest = firstRequest with
        {
            ExportedAtUtc = firstRequest.ExportedAtUtc.AddTicks(1),
        };

        PassportExportArtifact first = writer.Write(firstRequest);
        PassportExportArtifact second = writer.Write(secondRequest);

        Assert.NotEqual(first.FileName, second.FileName);
        Assert.DoesNotContain(firstRequest.ExportId, first.FileName, StringComparison.Ordinal);
        Assert.DoesNotContain(secondRequest.ExportId, second.FileName, StringComparison.Ordinal);
    }

    private static PassportExportWriteRequest CreateRequest(PassportExportFormat format)
    {
        Visit visit = Visit.Create(
            VisitId.Parse("01JTESTVISIT00000000000000"),
            "user-1",
            "park-1",
            VisitDate.ForDay(2026, 8, 31),
            "Europe/Paris",
            LocalServiceDayConvention.VisitStartLocalDate,
            "=1+1",
            "Souvenir, privé",
            NowUtc);
        visit.UpsertParkAssessment(RatingValue.FromHalfSteps(8), "Très belle journée", NowUtc);
        RideOccurrence occurrence = RideOccurrence.Create(
            RideOccurrenceId.Parse("01JTESTOCCURRENCE0000000000"),
            visit,
            "item-1",
            RideOccurrence.SortPositionStep,
            new OccurrenceMoment(new TimeOnly(14, 5), false),
            RideOccurrenceStatus.Completed,
            RideLogSource.Manual,
            HistoricalConsistency.Verified,
            null,
            "Premier rang",
            NowUtc);
        occurrence.UpsertAssessment(RatingValue.FromHalfSteps(9), "Intense", NowUtc);
        Park park = new Park
        {
            Id = "park-1",
            Name = "Europa Park",
            Status = ParkStatus.Operating,
        };
        VisitTarget target = new VisitTarget(
            "item-1",
            "park-1",
            "Silver Star",
            ParkItemCategory.Attraction,
            new DateOnly(2002, 3, 23),
            null,
            "Operating");
        return new PassportExportWriteRequest(
            "0123456789abcdef0123456789abcdef",
            format,
            NowUtc,
            new[] { visit },
            new[] { occurrence },
            new Dictionary<string, Park>(StringComparer.Ordinal) { [park.Id] = park },
            new Dictionary<string, VisitTarget>(StringComparer.Ordinal) { [target.ParkItemId] = target });
    }
}
