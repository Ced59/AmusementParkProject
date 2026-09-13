using System.IO.Compression;
using System.Text;
using System.Text.Json;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Services;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Application.Features.Sharing.Services;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Core.Domain.Sharing;
using AmusementPark.Core.Domain.Visits;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Passport.Services;

public sealed class CanonicalVisitExportWriterTests
{
    private const string VisitShareToken = "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8";
    private const string PassportShareToken = "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHhQ";
    private const string InvitationShareToken = "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHhA";
    private const string ComparisonShareToken = "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHhg";
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
    public void Write_CsvCreatesFifteenIndependentTablesAndSchemaMetadata()
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
                "comparison-invitations.csv",
                "comparison-missed-items.csv",
                "comparison-parks.csv",
                "comparison-ratings.csv",
                "comparison-years.csv",
                "comparisons.csv",
                "park-items.csv",
                "parks.csv",
                "passport-share-selections.csv",
                "ride-assessments.csv",
                "ride-occurrences.csv",
                "schema.json",
                "share-publications.csv",
                "share-snapshots.csv",
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

    [Theory]
    [InlineData(PassportExportFormat.Json)]
    [InlineData(PassportExportFormat.Csv)]
    public void Write_ShareLifecycleUsesReadableReferencesWithoutTechnicalIdentifiers(
        PassportExportFormat format)
    {
        CanonicalVisitExportWriter writer = new CanonicalVisitExportWriter();
        PassportExportWriteRequest request = CreateRequestWithShareLifecycle(format);

        PassportExportArtifact artifact = writer.Write(request);

        string content = ReadAllText(artifact);
        Assert.Contains("publication-0001", content, StringComparison.Ordinal);
        Assert.Contains("publication-0002", content, StringComparison.Ordinal);
        Assert.Contains("invitation-0001", content, StringComparison.Ordinal);
        Assert.Contains("comparison-0001", content, StringComparison.Ordinal);
        Assert.Contains("visit-0001", content, StringComparison.Ordinal);
        Assert.Contains("Europa Park", content, StringComparison.Ordinal);
        Assert.Contains("Other member", content, StringComparison.Ordinal);
        Assert.Contains("Revoked", content, StringComparison.Ordinal);
        Assert.Contains("2026-09-04", content, StringComparison.Ordinal);
        Assert.Contains("Public visit caption", content, StringComparison.Ordinal);
        Assert.Contains("Public passport caption", content, StringComparison.Ordinal);
        Assert.Contains("Selected Park", content, StringComparison.Ordinal);
        Assert.Contains("Selected Ride", content, StringComparison.Ordinal);
        Assert.Contains("ParkItem", content, StringComparison.Ordinal);
        Assert.DoesNotContain("publication-internal-visit", content, StringComparison.Ordinal);
        Assert.DoesNotContain("publication-internal-passport", content, StringComparison.Ordinal);
        Assert.DoesNotContain("invitation-internal", content, StringComparison.Ordinal);
        Assert.DoesNotContain("comparison-internal", content, StringComparison.Ordinal);
        Assert.DoesNotContain("other-passport-internal", content, StringComparison.Ordinal);
        Assert.DoesNotContain("park-internal-selection", content, StringComparison.Ordinal);
        Assert.DoesNotContain("rating-internal-selection", content, StringComparison.Ordinal);
        Assert.DoesNotContain("visit-content-fingerprint", content, StringComparison.Ordinal);
        Assert.DoesNotContain("passport-content-fingerprint", content, StringComparison.Ordinal);
        Assert.DoesNotContain("user-1", content, StringComparison.Ordinal);
        Assert.DoesNotContain("user-2", content, StringComparison.Ordinal);
        Assert.DoesNotContain(VisitShareToken, content, StringComparison.Ordinal);
        Assert.DoesNotContain(PassportShareToken, content, StringComparison.Ordinal);
        Assert.DoesNotContain(InvitationShareToken, content, StringComparison.Ordinal);
        Assert.DoesNotContain(ComparisonShareToken, content, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(PassportExportFormat.Json)]
    [InlineData(PassportExportFormat.Csv)]
    public void Write_WhenSelectedParkWasRenamed_PreservesFrozenSnapshotLabel(
        PassportExportFormat format)
    {
        PassportExportWriteRequest source = CreateRequestWithShareLifecycle(format);
        PassportProfileShareSnapshot snapshot = source.ShareLifecycle.PassportSnapshots.Single();
        PassportProfileShareSnapshot frozenSnapshot = snapshot with
        {
            Selection = snapshot.Selection with
            {
                SelectedParkIds = new[] { "park-1" },
            },
            Content = snapshot.Content with
            {
                Parks = new[]
                {
                    new PassportProfileShareParkResult(
                        "Frozen Selected Park",
                        "FR",
                        2,
                        2026,
                        2026,
                        4,
                        null),
                },
            },
        };
        source.Parks["park-1"].Name = "Current Renamed Park";
        PassportExportWriteRequest request = source with
        {
            ShareLifecycle = source.ShareLifecycle with
            {
                PassportSnapshots = new[] { frozenSnapshot },
            },
        };
        CanonicalVisitExportWriter writer = new CanonicalVisitExportWriter();

        PassportExportArtifact artifact = writer.Write(request);

        IReadOnlyCollection<string> selectionNames = ReadPassportSelectionParkNames(artifact);
        Assert.Equal(new[] { "Frozen Selected Park" }, selectionNames);
        Assert.DoesNotContain("Current Renamed Park", selectionNames);
    }

    [Theory]
    [InlineData(PassportExportFormat.Json)]
    [InlineData(PassportExportFormat.Csv)]
    public void Write_WhenSelectedParkIsOutsideFilteredYears_ExportsEverySelection(
        PassportExportFormat format)
    {
        PassportExportWriteRequest source = CreateRequestWithShareLifecycle(format);
        Visit olderVisit = Visit.Create(
            VisitId.Parse("01JTESTVISIT00000000000001"),
            source.UserId,
            "park-internal-selection",
            VisitDate.ForDay(2025, 8, 31),
            "Europe/Paris",
            LocalServiceDayConvention.VisitStartLocalDate,
            null,
            null,
            NowUtc.AddYears(-1));
        PassportProfileShareSnapshot snapshot = source.ShareLifecycle.PassportSnapshots.Single();
        PassportProfileShareSnapshot frozenSnapshot = snapshot with
        {
            Selection = snapshot.Selection with
            {
                SelectedParkIds = new[] { "park-1", "park-internal-selection" },
            },
            Content = snapshot.Content with
            {
                Parks = new[]
                {
                    new PassportProfileShareParkResult(
                        "Frozen In-Year Park",
                        "DE",
                        1,
                        2026,
                        2026,
                        2,
                        null),
                },
            },
        };
        PassportExportWriteRequest request = source with
        {
            Visits = source.Visits.Concat(new[] { olderVisit }).ToArray(),
            ShareLifecycle = source.ShareLifecycle with
            {
                PassportSnapshots = new[] { frozenSnapshot },
            },
        };
        CanonicalVisitExportWriter writer = new CanonicalVisitExportWriter();

        PassportExportArtifact artifact = writer.Write(request);

        IReadOnlyCollection<string> selectionNames = ReadPassportSelectionParkNames(artifact);
        Assert.Equal(new[] { "Frozen In-Year Park", "Selected Park" }, selectionNames);
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
            "user-1",
            format,
            NowUtc,
            new[] { visit },
            new[] { occurrence },
            new Dictionary<string, Park>(StringComparer.Ordinal) { [park.Id] = park },
            new Dictionary<string, VisitTarget>(StringComparer.Ordinal) { [target.ParkItemId] = target },
            PassportShareLifecycleExportData.Empty);
    }

    private static IReadOnlyCollection<string> ReadPassportSelectionParkNames(
        PassportExportArtifact artifact)
    {
        if (artifact.ContentType.StartsWith("application/json", StringComparison.Ordinal))
        {
            using JsonDocument document = JsonDocument.Parse(artifact.Content);
            JsonElement passportSnapshot = document.RootElement
                .GetProperty("shareSnapshots")
                .EnumerateArray()
                .Single(snapshot => string.Equals(
                    snapshot.GetProperty("type").GetString(),
                    "PassportProfile",
                    StringComparison.Ordinal));
            return passportSnapshot.GetProperty("selection").GetProperty("parks")
                .EnumerateArray()
                .Select(static park => park.GetProperty("name").GetString()!)
                .ToArray();
        }

        using MemoryStream stream = new MemoryStream(artifact.Content);
        using ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read);
        ZipArchiveEntry entry = archive.GetEntry("passport-share-selections.csv")!;
        using StreamReader reader = new StreamReader(entry.Open(), Encoding.UTF8);
        return reader.ReadToEnd()
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
            .Skip(1)
            .Select(static row => row.Split(','))
            .Where(static columns => string.Equals(columns[1], "Park", StringComparison.Ordinal))
            .Select(static columns => columns[4])
            .ToArray();
    }

    private static PassportExportWriteRequest CreateRequestWithShareLifecycle(
        PassportExportFormat format)
    {
        PassportExportWriteRequest source = CreateRequest(format);
        ShareContentPolicy visitPolicy = ShareContentPolicy.Create(
            SharePublicationType.VisitRecap,
            ShareDatePrecision.Day,
            new[] { ShareContentField.RideCount, ShareContentField.PublicCaption });
        SharePublication visitPublication = SharePublication.Create(
            SharePublicationId.Parse("publication-internal-visit"),
            source.UserId,
            SharePublicationType.VisitRecap,
            VisitRecapShareSourceScope.Create(source.UserId, source.Visits.Single().Id.Value),
            visitPolicy,
            4,
            NowUtc);
        visitPublication.Publish(
            ShareToken.Parse(VisitShareToken),
            ShareVisibility.Unlisted,
            4,
            visitPolicy,
            0,
            NowUtc.AddMinutes(1));
        ShareContentPolicy passportPolicy = ShareContentPolicy.Create(
            SharePublicationType.PassportProfile,
            ShareDatePrecision.Year,
            new[]
            {
                ShareContentField.PublicDisplayName,
                ShareContentField.RideCount,
                ShareContentField.GlobalRatings,
            });
        SharePublication passportPublication = SharePublication.Create(
            SharePublicationId.Parse("publication-internal-passport"),
            source.UserId,
            SharePublicationType.PassportProfile,
            PassportProfileShareSourceScope.Create(source.UserId),
            passportPolicy,
            8,
            NowUtc);
        passportPublication.Publish(
            ShareToken.Parse(PassportShareToken),
            ShareVisibility.Public,
            8,
            passportPolicy,
            0,
            NowUtc.AddMinutes(1));
        ProfileComparisonInvitation invitation = ProfileComparisonInvitation.Create(
            ProfileComparisonInvitationId.Parse("invitation-internal"),
            ShareToken.Parse(InvitationShareToken),
            source.UserId,
            passportPublication.Id,
            passportPublication.PublicationVersion,
            new[]
            {
                ProfileComparisonCategory.VisitedParks,
                ProfileComparisonCategory.PersonalRatings,
                ProfileComparisonCategory.YearlyActivity,
                ProfileComparisonCategory.MissedItems,
            },
            NowUtc.AddMinutes(2),
            NowUtc.AddDays(7));
        ProfileComparisonId comparisonId = ProfileComparisonId.Parse("comparison-internal");
        invitation.Accept(
            "user-2",
            SharePublicationId.Parse("other-passport-internal"),
            3,
            comparisonId,
            NowUtc.AddMinutes(3));
        ProfileComparisonCalculation calculation = new ProfileComparisonCalculation(
            "You",
            "Other member",
            invitation.Categories,
            new[] { new ProfileComparisonParkResult("Europa Park", "DE", 2, 3) },
            new[]
            {
                new ProfileComparisonRatingResult(
                    "Park",
                    "Europa Park",
                    null,
                    null,
                    4.5,
                    4,
                    0.5,
                    ProfileComparisonRatingAffinity.Close),
            },
            new[] { new ProfileComparisonYearResult(2026, 2, 3, 8, 9) },
            new[] { new ProfileComparisonMissedItemResult("Silver Star", "Operating", 0, 1) },
            1,
            ProfileComparisonCalculator.MinimumRatingsForCorrelation,
            null,
            false,
            ProfileComparisonCalculator.CalculationVersion);
        ProfileComparison comparison = ProfileComparison.Create(
            comparisonId,
            invitation.Id,
            ShareToken.Parse(ComparisonShareToken),
            source.UserId,
            "user-2",
            passportPublication.Id,
            passportPublication.PublicationVersion,
            SharePublicationId.Parse("other-passport-internal"),
            3,
            calculation,
            NowUtc.AddMinutes(3));
        comparison.Revoke(source.UserId, NowUtc.AddMinutes(4));
        VisitRecapShareSnapshot visitSnapshot = new VisitRecapShareSnapshot(
            visitPublication.Id,
            visitPublication.PublicationVersion,
            visitPublication.Version,
            visitPublication.SourceVersion,
            visitPolicy.SchemaVersion,
            visitPolicy.DatePrecision,
            visitPolicy.IncludedFields,
            "visit-content-fingerprint",
            new VisitRecapSharePreviewResult(
                "park-internal-selection",
                "Europa Park",
                null,
                1,
                2,
                new[] { "Attraction" },
                4.5,
                null,
                null,
                Array.Empty<VisitRecapShareItemResult>(),
                "Public visit caption",
                true,
                false,
                false),
            NowUtc.AddMinutes(1));
        PassportProfileShareInput passportSelection = new PassportProfileShareInput(
            new[] { 2026 },
            new[] { "park-internal-selection" },
            new[] { "rating-internal-selection" },
            "Public passport caption",
            ShareVisibility.Public,
            true);
        PassportProfileShareSnapshot passportSnapshot = new PassportProfileShareSnapshot(
            passportPublication.Id,
            passportPublication.PublicationVersion,
            passportPublication.Version,
            passportPublication.SourceVersion,
            passportPolicy.SchemaVersion,
            passportPolicy.DatePrecision,
            passportPolicy.IncludedFields,
            "passport-content-fingerprint",
            passportSelection,
            new PassportProfileSharePreviewResult(
                "You",
                null,
                "Public passport caption",
                ShareVisibility.Public,
                true,
                1,
                2,
                4,
                1,
                null,
                null,
                Array.Empty<PassportProfileShareCountryResult>(),
                Array.Empty<PassportProfileShareYearResult>(),
                Array.Empty<PassportProfileShareParkResult>(),
                new[]
                {
                    new PassportProfileShareRatingResult(
                        "ParkItem",
                        "Selected Ride",
                        "Selected Park",
                        "Attraction",
                        4.5),
                },
                Array.Empty<PassportProfileShareMissedItemResult>(),
                false,
                "passport-profile-v1",
                false),
            NowUtc.AddMinutes(1));
        Dictionary<string, Park> parks = source.Parks.ToDictionary(
            static pair => pair.Key,
            static pair => pair.Value,
            StringComparer.Ordinal);
        parks["park-internal-selection"] = new Park
        {
            Id = "park-internal-selection",
            Name = "Selected Park",
            CountryCode = "FR",
            Status = ParkStatus.Operating,
        };
        return source with
        {
            Parks = parks,
            ShareLifecycle = new PassportShareLifecycleExportData(
                new[] { visitPublication, passportPublication },
                new[] { invitation },
                new[] { comparison },
                new[] { visitSnapshot },
                Array.Empty<YearRecapShareSnapshot>(),
                new[] { passportSnapshot }),
        };
    }

    private static string ReadAllText(PassportExportArtifact artifact)
    {
        if (artifact.ContentType.StartsWith("application/json", StringComparison.Ordinal))
        {
            return Encoding.UTF8.GetString(artifact.Content);
        }

        using MemoryStream stream = new MemoryStream(artifact.Content);
        using ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read);
        StringBuilder content = new StringBuilder();
        foreach (ZipArchiveEntry entry in archive.Entries)
        {
            using StreamReader reader = new StreamReader(entry.Open(), Encoding.UTF8);
            content.Append(reader.ReadToEnd());
        }

        return content.ToString();
    }
}
