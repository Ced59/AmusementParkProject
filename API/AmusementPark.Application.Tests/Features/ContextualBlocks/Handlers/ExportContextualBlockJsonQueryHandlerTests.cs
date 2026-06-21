using System.Text.Json;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ContextualBlocks.Handlers;
using AmusementPark.Application.Features.ContextualBlocks.Queries;
using AmusementPark.Application.Features.ContextualBlocks.Results;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Localization;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.ContextualBlocks.Handlers;

public sealed class ExportContextualBlockJsonQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenDescriptionBlockExists_ShouldExportBoundedLocalizedJson()
    {
        Park park = new Park
        {
            Id = "park-1",
            Name = "Localized Park",
            CountryCode = "FR",
            City = "Paris",
            Descriptions = new List<LocalizedText>
            {
                new LocalizedText("fr", "Description francaise"),
                new LocalizedText("en", "English description"),
            },
            IsVisible = true,
            AdminReviewStatus = AdminReviewStatus.Validated,
        };

        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        parkRepository
            .Setup(repository => repository.GetByIdAsync("park-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(park);

        ExportContextualBlockJsonQueryHandler handler = new ExportContextualBlockJsonQueryHandler(
            parkRepository.Object,
            new Mock<IParkItemRepository>(MockBehavior.Strict).Object);

        ApplicationResult<ContextualBlockJsonExportResult> result = await handler.HandleAsync(
            new ExportContextualBlockJsonQuery("park.description", "park-1"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.EndsWith("-park-description-contextual-block.json", result.Value.FileName);

        using JsonDocument document = JsonDocument.Parse(result.Value.Json);
        JsonElement root = document.RootElement;
        JsonElement block = root.GetProperty("block");

        Assert.Equal("AmusementParkContextualBlockUpsert", root.GetProperty("documentType").GetString());
        Assert.Equal("park.description", root.GetProperty("blockType").GetString());
        Assert.Equal("Park", root.GetProperty("target").GetProperty("entityType").GetString());
        Assert.Equal("park-1", root.GetProperty("target").GetProperty("entityId").GetString());
        Assert.Equal("park-1", root.GetProperty("ids").GetProperty("parkId").GetString());
        Assert.Equal("park-1", block.GetProperty("parkId").GetString());
        AssertNoFullGraphProperties(root);
        AssertNoProperty(block, "countryCode");
        AssertNoProperty(block, "isVisible");
        AssertNoProperty(block, "adminReviewStatus");

        JsonElement descriptions = block.GetProperty("descriptions");
        List<string?> languageCodes = descriptions
            .EnumerateArray()
            .Select(static description => description.GetProperty("languageCode").GetString())
            .ToList();

        Assert.Equal(new[] { "en", "fr", "es", "de", "it", "pl", "nl", "pt" }, languageCodes);
        JsonElement frenchDescription = descriptions
            .EnumerateArray()
            .Single(static description => description.GetProperty("languageCode").GetString() == "fr");
        JsonElement germanDescription = descriptions
            .EnumerateArray()
            .Single(static description => description.GetProperty("languageCode").GetString() == "de");
        Assert.Equal("Description francaise", frenchDescription.GetProperty("value").GetString());
        Assert.Equal(JsonValueKind.Null, germanDescription.GetProperty("value").ValueKind);

        parkRepository.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenPracticalBlockExists_ShouldExportOnlyPracticalFieldsAndAttachmentIds()
    {
        Park park = new Park
        {
            Id = "park-1",
            Name = "Practical Park",
            CountryCode = "DE",
            City = "Bruehl",
            Street = "Berggeiststrasse",
            PostalCode = "50321",
            WebsiteUrl = "https://example.test",
            FounderId = "founder-1",
            OperatorId = "operator-1",
            Descriptions = new List<LocalizedText>
            {
                new LocalizedText("en", "Should not be exported here"),
            },
            IsVisible = false,
            AdminReviewStatus = AdminReviewStatus.ToReview,
        };
        park.SetPosition(50.8, 6.9);

        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        parkRepository
            .Setup(repository => repository.GetByIdAsync("park-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(park);

        ExportContextualBlockJsonQueryHandler handler = new ExportContextualBlockJsonQueryHandler(
            parkRepository.Object,
            new Mock<IParkItemRepository>(MockBehavior.Strict).Object);

        ApplicationResult<ContextualBlockJsonExportResult> result = await handler.HandleAsync(
            new ExportContextualBlockJsonQuery("park.practical", "park-1"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.EndsWith("-park-practical-contextual-block.json", result.Value.FileName);

        using JsonDocument document = JsonDocument.Parse(result.Value.Json);
        JsonElement root = document.RootElement;
        JsonElement ids = root.GetProperty("ids");
        JsonElement block = root.GetProperty("block");

        Assert.Equal("park.practical", root.GetProperty("blockType").GetString());
        Assert.Equal("park-1", ids.GetProperty("parkId").GetString());
        Assert.Equal("founder-1", ids.GetProperty("founderId").GetString());
        Assert.Equal("operator-1", ids.GetProperty("operatorId").GetString());
        Assert.Equal("DE", block.GetProperty("countryCode").GetString());
        Assert.Equal("Bruehl", block.GetProperty("city").GetString());
        Assert.Equal("Berggeiststrasse", block.GetProperty("street").GetString());
        Assert.Equal("50321", block.GetProperty("postalCode").GetString());
        Assert.Equal("https://example.test", block.GetProperty("websiteUrl").GetString());
        Assert.Equal(50.8, block.GetProperty("latitude").GetDouble(), 3);
        Assert.Equal(6.9, block.GetProperty("longitude").GetDouble(), 3);
        AssertNoFullGraphProperties(root);
        AssertNoProperty(block, "descriptions");
        AssertNoProperty(block, "name");
        AssertNoProperty(block, "isVisible");
        AssertNoProperty(block, "adminReviewStatus");

        parkRepository.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenParkLocationBlockExists_ShouldExportOnlyCoordinates()
    {
        Park park = new Park
        {
            Id = "park-1",
            Name = "Location Park",
            CountryCode = "BE",
            City = "Ypres",
            Descriptions = new List<LocalizedText>
            {
                new LocalizedText("en", "Should not be exported here"),
            },
            IsVisible = true,
            AdminReviewStatus = AdminReviewStatus.Validated,
        };

        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        parkRepository
            .Setup(repository => repository.GetByIdAsync("park-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(park);

        ExportContextualBlockJsonQueryHandler handler = new ExportContextualBlockJsonQueryHandler(
            parkRepository.Object,
            new Mock<IParkItemRepository>(MockBehavior.Strict).Object);

        ApplicationResult<ContextualBlockJsonExportResult> result = await handler.HandleAsync(
            new ExportContextualBlockJsonQuery("park.location", "park-1"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.EndsWith("-park-location-contextual-block.json", result.Value.FileName);

        using JsonDocument document = JsonDocument.Parse(result.Value.Json);
        JsonElement root = document.RootElement;
        JsonElement block = root.GetProperty("block");

        Assert.Equal("park.location", root.GetProperty("blockType").GetString());
        Assert.Equal("Park", root.GetProperty("target").GetProperty("entityType").GetString());
        Assert.Equal("park-1", root.GetProperty("target").GetProperty("entityId").GetString());
        Assert.Equal("park-1", root.GetProperty("ids").GetProperty("parkId").GetString());
        Assert.Equal("park-1", block.GetProperty("parkId").GetString());
        Assert.Equal(JsonValueKind.Null, block.GetProperty("latitude").ValueKind);
        Assert.Equal(JsonValueKind.Null, block.GetProperty("longitude").ValueKind);
        AssertNoFullGraphProperties(root);
        AssertNoProperty(block, "descriptions");
        AssertNoProperty(block, "city");
        AssertNoProperty(block, "countryCode");

        parkRepository.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenParkItemDescriptionBlockExists_ShouldExportBoundedLocalizedJsonWithAttachmentIds()
    {
        ParkItem item = new ParkItem
        {
            Id = "item-1",
            ParkId = "park-1",
            ZoneId = "zone-1",
            Name = "Wakala",
            Descriptions = new List<LocalizedText>
            {
                new LocalizedText("fr", "Description francaise"),
                new LocalizedText("en", "English description"),
            },
            IsVisible = true,
            AdminReviewStatus = AdminReviewStatus.Validated,
        };

        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        parkItemRepository
            .Setup(repository => repository.GetByIdAsync("item-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        ExportContextualBlockJsonQueryHandler handler = new ExportContextualBlockJsonQueryHandler(parkRepository.Object, parkItemRepository.Object);

        ApplicationResult<ContextualBlockJsonExportResult> result = await handler.HandleAsync(
            new ExportContextualBlockJsonQuery("parkItem.description", "item-1"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.EndsWith("-parkItem-description-contextual-block.json", result.Value.FileName);

        using JsonDocument document = JsonDocument.Parse(result.Value.Json);
        JsonElement root = document.RootElement;
        JsonElement block = root.GetProperty("block");

        Assert.Equal("parkItem.description", root.GetProperty("blockType").GetString());
        Assert.Equal("ParkItem", root.GetProperty("target").GetProperty("entityType").GetString());
        Assert.Equal("item-1", root.GetProperty("target").GetProperty("entityId").GetString());
        Assert.Equal("park-1", root.GetProperty("ids").GetProperty("parkId").GetString());
        Assert.Equal("item-1", root.GetProperty("ids").GetProperty("parkItemId").GetString());
        Assert.Equal("zone-1", root.GetProperty("ids").GetProperty("zoneId").GetString());
        Assert.Equal("park-1", block.GetProperty("parkId").GetString());
        Assert.Equal("item-1", block.GetProperty("parkItemId").GetString());
        Assert.Equal("zone-1", block.GetProperty("zoneId").GetString());
        AssertNoFullGraphProperties(root);
        AssertNoProperty(block, "name");
        AssertNoProperty(block, "category");
        AssertNoProperty(block, "isVisible");
        AssertNoProperty(block, "adminReviewStatus");

        JsonElement descriptions = block.GetProperty("descriptions");
        List<string?> languageCodes = descriptions
            .EnumerateArray()
            .Select(static description => description.GetProperty("languageCode").GetString())
            .ToList();

        Assert.Equal(new[] { "en", "fr", "es", "de", "it", "pl", "nl", "pt" }, languageCodes);
        JsonElement frenchDescription = descriptions
            .EnumerateArray()
            .Single(static description => description.GetProperty("languageCode").GetString() == "fr");
        Assert.Equal("Description francaise", frenchDescription.GetProperty("value").GetString());

        parkRepository.VerifyNoOtherCalls();
        parkItemRepository.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenParkItemLocationBlockExists_ShouldExportOnlyCoordinatesAndAttachmentIds()
    {
        ParkItem item = new ParkItem
        {
            Id = "item-1",
            ParkId = "park-1",
            ZoneId = "zone-1",
            Name = "Wakala",
            Descriptions = new List<LocalizedText>
            {
                new LocalizedText("fr", "Should not be exported here"),
            },
            IsVisible = true,
            AdminReviewStatus = AdminReviewStatus.Validated,
        };
        item.SetPosition(50.811, 2.933);

        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        parkItemRepository
            .Setup(repository => repository.GetByIdAsync("item-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        ExportContextualBlockJsonQueryHandler handler = new ExportContextualBlockJsonQueryHandler(parkRepository.Object, parkItemRepository.Object);

        ApplicationResult<ContextualBlockJsonExportResult> result = await handler.HandleAsync(
            new ExportContextualBlockJsonQuery("parkItem.location", "item-1"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.EndsWith("-parkItem-location-contextual-block.json", result.Value.FileName);

        using JsonDocument document = JsonDocument.Parse(result.Value.Json);
        JsonElement root = document.RootElement;
        JsonElement block = root.GetProperty("block");

        Assert.Equal("parkItem.location", root.GetProperty("blockType").GetString());
        Assert.Equal("ParkItem", root.GetProperty("target").GetProperty("entityType").GetString());
        Assert.Equal("item-1", root.GetProperty("target").GetProperty("entityId").GetString());
        Assert.Equal("park-1", root.GetProperty("ids").GetProperty("parkId").GetString());
        Assert.Equal("item-1", root.GetProperty("ids").GetProperty("parkItemId").GetString());
        Assert.Equal("zone-1", root.GetProperty("ids").GetProperty("zoneId").GetString());
        Assert.Equal("park-1", block.GetProperty("parkId").GetString());
        Assert.Equal("item-1", block.GetProperty("parkItemId").GetString());
        Assert.Equal("zone-1", block.GetProperty("zoneId").GetString());
        Assert.Equal(50.811, block.GetProperty("latitude").GetDouble(), 3);
        Assert.Equal(2.933, block.GetProperty("longitude").GetDouble(), 3);
        AssertNoFullGraphProperties(root);
        AssertNoProperty(block, "descriptions");
        AssertNoProperty(block, "name");
        AssertNoProperty(block, "category");

        parkRepository.VerifyNoOtherCalls();
        parkItemRepository.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenBlockTypeIsUnsupported_ShouldReturnValidationWithoutLoadingPark()
    {
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        ExportContextualBlockJsonQueryHandler handler = new ExportContextualBlockJsonQueryHandler(parkRepository.Object, parkItemRepository.Object);

        ApplicationResult<ContextualBlockJsonExportResult> result = await handler.HandleAsync(
            new ExportContextualBlockJsonQuery("park.hero", "park-1"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorType.Validation, result.Errors.Single().Type);
        parkRepository.VerifyNoOtherCalls();
        parkItemRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenParkDoesNotExist_ShouldReturnNotFound()
    {
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        parkRepository
            .Setup(repository => repository.GetByIdAsync("missing", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Park?)null);

        ExportContextualBlockJsonQueryHandler handler = new ExportContextualBlockJsonQueryHandler(
            parkRepository.Object,
            new Mock<IParkItemRepository>(MockBehavior.Strict).Object);

        ApplicationResult<ContextualBlockJsonExportResult> result = await handler.HandleAsync(
            new ExportContextualBlockJsonQuery("park.description", "missing"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorType.NotFound, result.Errors.Single().Type);
        parkRepository.VerifyAll();
    }

    private static void AssertNoFullGraphProperties(JsonElement root)
    {
        AssertNoProperty(root, "references");
        AssertNoProperty(root, "zones");
        AssertNoProperty(root, "items");
        AssertNoProperty(root, "images");
    }

    private static void AssertNoProperty(JsonElement element, string propertyName)
    {
        Assert.False(element.TryGetProperty(propertyName, out JsonElement _), $"Property '{propertyName}' should not be exported.");
    }
}
