using System.Globalization;
using System.Text.Json;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ContextualBlocks.Commands;
using AmusementPark.Application.Features.ContextualBlocks.Handlers;
using AmusementPark.Application.Features.ContextualBlocks.Results;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Localization;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.ContextualBlocks.Handlers;

public sealed class PreviewContextualBlockJsonCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenDescriptionJsonChangesOneLanguage_ShouldReturnReadablePreview()
    {
        Park park = CreatePark();
        Mock<IParkRepository> parkRepository = CreateRepository(park);
        PreviewContextualBlockJsonCommandHandler handler = CreateHandler(parkRepository);

        using JsonDocument document = JsonDocument.Parse(BuildDescriptionDocument("park-1", new Dictionary<string, string?>
        {
            ["en"] = "English description",
            ["fr"] = "Description francaise mise a jour",
            ["es"] = null,
            ["de"] = null,
            ["it"] = null,
            ["pl"] = null,
            ["nl"] = null,
            ["pt"] = null,
        }));

        ApplicationResult<ContextualBlockPreviewResult> result = await handler.HandleAsync(
            new PreviewContextualBlockJsonCommand("park.description", "park-1", document.RootElement),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.True(result.Value.CanApply);
        Assert.Empty(result.Value.Errors);
        ContextualBlockPreviewChange change = Assert.Single(result.Value.Changes);
        Assert.Equal("descriptions.fr.value", change.Field);
        Assert.Equal("fr", change.LanguageCode);
        Assert.Equal("Description francaise", change.OldValue);
        Assert.Equal("Description francaise mise a jour", change.NewValue);
        Assert.Equal(1, result.Value.Counts.Updated);
        Assert.Equal(7, result.Value.Counts.Unchanged);
        parkRepository.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenDescriptionJsonContainsOutOfScopeField_ShouldRejectBeforeDiff()
    {
        Park park = CreatePark();
        Mock<IParkRepository> parkRepository = CreateRepository(park);
        PreviewContextualBlockJsonCommandHandler handler = CreateHandler(parkRepository);

        string json = BuildDescriptionDocument("park-1", BuildAllDescriptions());
        json = json.Replace("\"descriptions\":", "\"name\":\"Forbidden\",\"descriptions\":", StringComparison.Ordinal);
        using JsonDocument document = JsonDocument.Parse(json);

        ApplicationResult<ContextualBlockPreviewResult> result = await handler.HandleAsync(
            new PreviewContextualBlockJsonCommand("park.description", "park-1", document.RootElement),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.False(result.Value.CanApply);
        Assert.Contains(result.Value.Errors, static error => error.Contains("block.name", StringComparison.Ordinal));
        Assert.Empty(result.Value.Changes);
        Assert.Equal(1, result.Value.Counts.Errors);
        parkRepository.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenLocalizedDescriptionIsMissingLanguage_ShouldReturnLanguageError()
    {
        Park park = CreatePark();
        Mock<IParkRepository> parkRepository = CreateRepository(park);
        PreviewContextualBlockJsonCommandHandler handler = CreateHandler(parkRepository);
        Dictionary<string, string?> descriptions = BuildAllDescriptions();
        descriptions.Remove("de");
        using JsonDocument document = JsonDocument.Parse(BuildDescriptionDocument("park-1", descriptions));

        ApplicationResult<ContextualBlockPreviewResult> result = await handler.HandleAsync(
            new PreviewContextualBlockJsonCommand("park.description", "park-1", document.RootElement),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.False(result.Value.CanApply);
        Assert.Contains(result.Value.Errors, static error => error.Contains("Langue 'de' manquante", StringComparison.Ordinal));
        Assert.Empty(result.Value.Changes);
        parkRepository.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenDocumentTargetsAnotherPark_ShouldRejectAttachment()
    {
        Park park = CreatePark();
        Mock<IParkRepository> parkRepository = CreateRepository(park);
        PreviewContextualBlockJsonCommandHandler handler = CreateHandler(parkRepository);
        using JsonDocument document = JsonDocument.Parse(BuildDescriptionDocument("other-park", BuildAllDescriptions()));

        ApplicationResult<ContextualBlockPreviewResult> result = await handler.HandleAsync(
            new PreviewContextualBlockJsonCommand("park.description", "park-1", document.RootElement),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.False(result.Value.CanApply);
        Assert.Contains(result.Value.Errors, static error => error.Contains("target.entityId", StringComparison.Ordinal));
        Assert.Contains(result.Value.Errors, static error => error.Contains("ids.parkId", StringComparison.Ordinal));
        Assert.Contains(result.Value.Errors, static error => error.Contains("block.parkId", StringComparison.Ordinal));
        Assert.Empty(result.Value.Changes);
        parkRepository.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenPracticalJsonChangesCity_ShouldPreviewOnlyPracticalField()
    {
        Park park = CreatePark();
        Mock<IParkRepository> parkRepository = CreateRepository(park);
        PreviewContextualBlockJsonCommandHandler handler = CreateHandler(parkRepository);

        using JsonDocument document = JsonDocument.Parse(
            """
            {
              "documentType": "AmusementParkContextualBlockUpsert",
              "schemaVersion": "2026-06-21",
              "blockType": "park.practical",
              "target": { "entityType": "Park", "entityId": "park-1" },
              "ids": { "parkId": "park-1", "founderId": "founder-1", "operatorId": "operator-1" },
              "block": {
                "parkId": "park-1",
                "countryCode": "FR",
                "city": "Lyon",
                "street": "Rue A",
                "postalCode": "75000",
                "websiteUrl": "https://example.test",
                "founderId": "founder-1",
                "operatorId": "operator-1",
                "latitude": 48.85,
                "longitude": 2.35
              }
            }
            """);

        ApplicationResult<ContextualBlockPreviewResult> result = await handler.HandleAsync(
            new PreviewContextualBlockJsonCommand("park.practical", "park-1", document.RootElement),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.True(result.Value.CanApply);
        ContextualBlockPreviewChange change = Assert.Single(result.Value.Changes);
        Assert.Equal("city", change.Field);
        Assert.Equal("Paris", change.OldValue);
        Assert.Equal("Lyon", change.NewValue);
        Assert.DoesNotContain(result.Value.Changes, static value => value.Field == "descriptions.fr.value");
        parkRepository.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenPracticalJsonContainsInvalidCoordinates_ShouldRejectBeforeApply()
    {
        Park park = CreatePark();
        Mock<IParkRepository> parkRepository = CreateRepository(park);
        PreviewContextualBlockJsonCommandHandler handler = CreateHandler(parkRepository);

        using JsonDocument document = JsonDocument.Parse(
            """
            {
              "documentType": "AmusementParkContextualBlockUpsert",
              "schemaVersion": "2026-06-21",
              "blockType": "park.practical",
              "target": { "entityType": "Park", "entityId": "park-1" },
              "ids": { "parkId": "park-1", "founderId": "founder-1", "operatorId": "operator-1" },
              "block": {
                "parkId": "park-1",
                "latitude": 91,
                "longitude": 2.35
              }
            }
            """);

        ApplicationResult<ContextualBlockPreviewResult> result = await handler.HandleAsync(
            new PreviewContextualBlockJsonCommand("park.practical", "park-1", document.RootElement),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.False(result.Value.CanApply);
        Assert.Contains(result.Value.Errors, static error => error.Contains("block.latitude", StringComparison.Ordinal));
        Assert.Empty(result.Value.Changes);
        parkRepository.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenParkLocationJsonChangesCoordinates_ShouldPreviewOnlyLocationFields()
    {
        Park park = CreatePark();
        Mock<IParkRepository> parkRepository = CreateRepository(park);
        PreviewContextualBlockJsonCommandHandler handler = CreateHandler(parkRepository);

        using JsonDocument document = JsonDocument.Parse(BuildParkLocationDocument("park-1", 50.85, 4.35));

        ApplicationResult<ContextualBlockPreviewResult> result = await handler.HandleAsync(
            new PreviewContextualBlockJsonCommand("park.location", "park-1", document.RootElement),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.True(result.Value.CanApply);
        Assert.Empty(result.Value.Errors);
        Assert.Equal(2, result.Value.Changes.Count);
        ContextualBlockPreviewChange latitudeChange = result.Value.Changes.Single(static change => change.Field == "latitude");
        ContextualBlockPreviewChange longitudeChange = result.Value.Changes.Single(static change => change.Field == "longitude");
        Assert.Equal(48.85, double.Parse(latitudeChange.OldValue!, CultureInfo.InvariantCulture), 3);
        Assert.Equal(50.85, double.Parse(latitudeChange.NewValue!, CultureInfo.InvariantCulture), 3);
        Assert.Equal(2.35, double.Parse(longitudeChange.OldValue!, CultureInfo.InvariantCulture), 3);
        Assert.Equal(4.35, double.Parse(longitudeChange.NewValue!, CultureInfo.InvariantCulture), 3);
        Assert.DoesNotContain(result.Value.Changes, static change => change.Field == "city");
        parkRepository.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenParkItemDescriptionJsonChangesOneLanguage_ShouldReturnReadablePreview()
    {
        ParkItem item = CreateParkItem();
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> parkItemRepository = CreateParkItemRepository(item);
        PreviewContextualBlockJsonCommandHandler handler = CreateHandler(parkRepository, parkItemRepository);

        using JsonDocument document = JsonDocument.Parse(BuildParkItemDescriptionDocument("park-1", "item-1", "zone-1", new Dictionary<string, string?>
        {
            ["en"] = "English item description",
            ["fr"] = "Description item mise a jour",
            ["es"] = null,
            ["de"] = null,
            ["it"] = null,
            ["pl"] = null,
            ["nl"] = null,
            ["pt"] = null,
        }));

        ApplicationResult<ContextualBlockPreviewResult> result = await handler.HandleAsync(
            new PreviewContextualBlockJsonCommand("parkItem.description", "item-1", document.RootElement),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.True(result.Value.CanApply);
        Assert.Empty(result.Value.Errors);
        Assert.Equal("ParkItem", result.Value.Target.EntityType);
        Assert.Equal("item-1", result.Value.Target.EntityId);
        ContextualBlockPreviewChange change = Assert.Single(result.Value.Changes);
        Assert.Equal("descriptions.fr.value", change.Field);
        Assert.Equal("fr", change.LanguageCode);
        Assert.Equal("Description item francaise", change.OldValue);
        Assert.Equal("Description item mise a jour", change.NewValue);
        parkRepository.VerifyNoOtherCalls();
        parkItemRepository.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenParkItemDescriptionTargetsAnotherItem_ShouldRejectAttachment()
    {
        ParkItem item = CreateParkItem();
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> parkItemRepository = CreateParkItemRepository(item);
        PreviewContextualBlockJsonCommandHandler handler = CreateHandler(parkRepository, parkItemRepository);

        using JsonDocument document = JsonDocument.Parse(BuildParkItemDescriptionDocument("park-1", "other-item", "zone-1", BuildAllItemDescriptions()));

        ApplicationResult<ContextualBlockPreviewResult> result = await handler.HandleAsync(
            new PreviewContextualBlockJsonCommand("parkItem.description", "item-1", document.RootElement),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.False(result.Value.CanApply);
        Assert.Contains(result.Value.Errors, static error => error.Contains("target.entityId", StringComparison.Ordinal));
        Assert.Contains(result.Value.Errors, static error => error.Contains("ids.parkItemId", StringComparison.Ordinal));
        Assert.Contains(result.Value.Errors, static error => error.Contains("block.parkItemId", StringComparison.Ordinal));
        Assert.Empty(result.Value.Changes);
        parkRepository.VerifyNoOtherCalls();
        parkItemRepository.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenParkItemLocationJsonAddsCoordinates_ShouldPreviewParkItemLocation()
    {
        ParkItem item = CreateParkItem();
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> parkItemRepository = CreateParkItemRepository(item);
        PreviewContextualBlockJsonCommandHandler handler = CreateHandler(parkRepository, parkItemRepository);

        using JsonDocument document = JsonDocument.Parse(BuildParkItemLocationDocument("park-1", "item-1", "zone-1", 50.811, 2.933));

        ApplicationResult<ContextualBlockPreviewResult> result = await handler.HandleAsync(
            new PreviewContextualBlockJsonCommand("parkItem.location", "item-1", document.RootElement),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.True(result.Value.CanApply);
        Assert.Equal("ParkItem", result.Value.Target.EntityType);
        Assert.Equal("item-1", result.Value.Target.EntityId);
        Assert.Equal(2, result.Value.Changes.Count);
        ContextualBlockPreviewChange latitudeChange = result.Value.Changes.Single(static change => change.Field == "latitude");
        ContextualBlockPreviewChange longitudeChange = result.Value.Changes.Single(static change => change.Field == "longitude");
        Assert.Null(latitudeChange.OldValue);
        Assert.Null(longitudeChange.OldValue);
        Assert.Equal(50.811, double.Parse(latitudeChange.NewValue!, CultureInfo.InvariantCulture), 3);
        Assert.Equal(2.933, double.Parse(longitudeChange.NewValue!, CultureInfo.InvariantCulture), 3);
        parkRepository.VerifyNoOtherCalls();
        parkItemRepository.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenLocationJsonContainsPartialCoordinates_ShouldRejectBeforeApply()
    {
        Park park = CreatePark();
        Mock<IParkRepository> parkRepository = CreateRepository(park);
        PreviewContextualBlockJsonCommandHandler handler = CreateHandler(parkRepository);

        using JsonDocument document = JsonDocument.Parse(BuildParkLocationDocument("park-1", 48.85, null));

        ApplicationResult<ContextualBlockPreviewResult> result = await handler.HandleAsync(
            new PreviewContextualBlockJsonCommand("park.location", "park-1", document.RootElement),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.False(result.Value.CanApply);
        Assert.Contains(result.Value.Errors, static error => error.Contains("block.latitude et block.longitude", StringComparison.Ordinal));
        Assert.Empty(result.Value.Changes);
        parkRepository.VerifyAll();
    }

    private static PreviewContextualBlockJsonCommandHandler CreateHandler(
        Mock<IParkRepository> parkRepository,
        Mock<IParkItemRepository>? parkItemRepository = null)
    {
        return new PreviewContextualBlockJsonCommandHandler(
            parkRepository.Object,
            (parkItemRepository ?? new Mock<IParkItemRepository>(MockBehavior.Strict)).Object);
    }

    private static Mock<IParkRepository> CreateRepository(Park park)
    {
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        parkRepository
            .Setup(repository => repository.GetByIdAsync("park-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(park);
        return parkRepository;
    }

    private static Mock<IParkItemRepository> CreateParkItemRepository(ParkItem item)
    {
        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        parkItemRepository
            .Setup(repository => repository.GetByIdAsync("item-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);
        return parkItemRepository;
    }

    private static Park CreatePark()
    {
        Park park = new Park
        {
            Id = "park-1",
            Name = "Preview Park",
            CountryCode = "FR",
            City = "Paris",
            Street = "Rue A",
            PostalCode = "75000",
            WebsiteUrl = "https://example.test",
            FounderId = "founder-1",
            OperatorId = "operator-1",
            Descriptions = new List<LocalizedText>
            {
                new LocalizedText("en", "English description"),
                new LocalizedText("fr", "Description francaise"),
            },
        };
        park.SetPosition(48.85, 2.35);
        return park;
    }

    private static ParkItem CreateParkItem()
    {
        return new ParkItem
        {
            Id = "item-1",
            ParkId = "park-1",
            ZoneId = "zone-1",
            Name = "Preview Item",
            Descriptions = new List<LocalizedText>
            {
                new LocalizedText("en", "English item description"),
                new LocalizedText("fr", "Description item francaise"),
            },
        };
    }

    private static Dictionary<string, string?> BuildAllDescriptions()
    {
        return new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["en"] = "English description",
            ["fr"] = "Description francaise",
            ["es"] = null,
            ["de"] = null,
            ["it"] = null,
            ["pl"] = null,
            ["nl"] = null,
            ["pt"] = null,
        };
    }

    private static Dictionary<string, string?> BuildAllItemDescriptions()
    {
        return new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["en"] = "English item description",
            ["fr"] = "Description item francaise",
            ["es"] = null,
            ["de"] = null,
            ["it"] = null,
            ["pl"] = null,
            ["nl"] = null,
            ["pt"] = null,
        };
    }

    private static string BuildDescriptionDocument(string parkId, IReadOnlyDictionary<string, string?> descriptions)
    {
        string descriptionJson = string.Join(
            ",",
            descriptions.Select(static entry =>
            {
                string value = entry.Value is null ? "null" : JsonSerializer.Serialize(entry.Value);
                return $$"""{ "languageCode": "{{entry.Key}}", "value": {{value}} }""";
            }));

        return $$"""
        {
          "documentType": "AmusementParkContextualBlockUpsert",
          "schemaVersion": "2026-06-21",
          "blockType": "park.description",
          "target": { "entityType": "Park", "entityId": "{{parkId}}" },
          "ids": { "parkId": "{{parkId}}" },
          "block": {
            "parkId": "{{parkId}}",
            "descriptions": [{{descriptionJson}}]
          }
        }
        """;
    }

    private static string BuildParkItemDescriptionDocument(string parkId, string parkItemId, string? zoneId, IReadOnlyDictionary<string, string?> descriptions)
    {
        string descriptionJson = string.Join(
            ",",
            descriptions.Select(static entry =>
            {
                string value = entry.Value is null ? "null" : JsonSerializer.Serialize(entry.Value);
                return $$"""{ "languageCode": "{{entry.Key}}", "value": {{value}} }""";
            }));
        string zoneIdJson = zoneId is null ? string.Empty : $", \"zoneId\": \"{zoneId}\"";

        return $$"""
        {
          "documentType": "AmusementParkContextualBlockUpsert",
          "schemaVersion": "2026-06-21",
          "blockType": "parkItem.description",
          "target": { "entityType": "ParkItem", "entityId": "{{parkItemId}}" },
          "ids": { "parkId": "{{parkId}}", "parkItemId": "{{parkItemId}}"{{zoneIdJson}} },
          "block": {
            "parkId": "{{parkId}}",
            "parkItemId": "{{parkItemId}}"{{zoneIdJson}},
            "descriptions": [{{descriptionJson}}]
          }
        }
        """;
    }

    private static string BuildParkLocationDocument(string parkId, double? latitude, double? longitude)
    {
        string latitudeJson = latitude.HasValue ? JsonSerializer.Serialize(latitude.Value) : "null";
        string longitudeJson = longitude.HasValue ? JsonSerializer.Serialize(longitude.Value) : "null";

        return $$"""
        {
          "documentType": "AmusementParkContextualBlockUpsert",
          "schemaVersion": "2026-06-21",
          "blockType": "park.location",
          "target": { "entityType": "Park", "entityId": "{{parkId}}" },
          "ids": { "parkId": "{{parkId}}" },
          "block": {
            "parkId": "{{parkId}}",
            "latitude": {{latitudeJson}},
            "longitude": {{longitudeJson}}
          }
        }
        """;
    }

    private static string BuildParkItemLocationDocument(string parkId, string parkItemId, string? zoneId, double? latitude, double? longitude)
    {
        string zoneIdJson = zoneId is null ? string.Empty : $", \"zoneId\": \"{zoneId}\"";
        string latitudeJson = latitude.HasValue ? JsonSerializer.Serialize(latitude.Value) : "null";
        string longitudeJson = longitude.HasValue ? JsonSerializer.Serialize(longitude.Value) : "null";

        return $$"""
        {
          "documentType": "AmusementParkContextualBlockUpsert",
          "schemaVersion": "2026-06-21",
          "blockType": "parkItem.location",
          "target": { "entityType": "ParkItem", "entityId": "{{parkItemId}}" },
          "ids": { "parkId": "{{parkId}}", "parkItemId": "{{parkItemId}}"{{zoneIdJson}} },
          "block": {
            "parkId": "{{parkId}}",
            "parkItemId": "{{parkItemId}}"{{zoneIdJson}},
            "latitude": {{latitudeJson}},
            "longitude": {{longitudeJson}}
          }
        }
        """;
    }
}
