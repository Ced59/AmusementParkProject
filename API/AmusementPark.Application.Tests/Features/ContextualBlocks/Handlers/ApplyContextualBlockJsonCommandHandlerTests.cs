using System.Text.Json;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ContextualBlocks.Commands;
using AmusementPark.Application.Features.ContextualBlocks.Handlers;
using AmusementPark.Application.Features.ContextualBlocks.Results;
using AmusementPark.Application.Features.Parks.Commands;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Localization;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.ContextualBlocks.Handlers;

public sealed class ApplyContextualBlockJsonCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenDescriptionJsonIsValid_ShouldUpdateLocalizedDescriptions()
    {
        Park park = CreatePark();
        Mock<IParkRepository> parkRepository = CreateRepository(park);
        Mock<ICommandHandler<UpdateParkCommand, ApplicationResult<Park>>> updateParkHandler = CreateUpdateHandler();
        ApplyContextualBlockJsonCommandHandler handler = CreateHandler(parkRepository, updateParkHandler);

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
            new ApplyContextualBlockJsonCommand("park.description", "park-1", document.RootElement),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.True(result.Value.IsApplied);
        Assert.True(result.Value.CanApply);
        Assert.Empty(result.Value.Errors);
        Assert.Contains(park.Descriptions, static description => description.LanguageCode == "fr" && description.Value == "Description francaise mise a jour");
        Assert.Contains(park.Descriptions, static description => description.LanguageCode == "pt" && description.Value is null);
        Assert.Equal(8, park.Descriptions.Count);
        updateParkHandler.Verify(
            handlerMock => handlerMock.HandleAsync(
                It.Is<UpdateParkCommand>(command => command.ParkId == "park-1" && command.Park.Descriptions.Count == 8),
                It.IsAny<CancellationToken>()),
            Times.Once);
        parkRepository.Verify(
            repository => repository.GetByIdAsync("park-1", true, It.IsAny<CancellationToken>()),
            Times.Exactly(2));
        parkRepository.VerifyAll();
        updateParkHandler.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenPreviewRejectsJson_ShouldNotUpdatePark()
    {
        Park park = CreatePark();
        Mock<IParkRepository> parkRepository = CreateRepository(park);
        Mock<ICommandHandler<UpdateParkCommand, ApplicationResult<Park>>> updateParkHandler = CreateUpdateHandler();
        ApplyContextualBlockJsonCommandHandler handler = CreateHandler(parkRepository, updateParkHandler);

        string json = BuildDescriptionDocument("park-1", BuildAllDescriptions());
        json = json.Replace("\"descriptions\":", "\"name\":\"Forbidden\",\"descriptions\":", StringComparison.Ordinal);
        using JsonDocument document = JsonDocument.Parse(json);

        ApplicationResult<ContextualBlockPreviewResult> result = await handler.HandleAsync(
            new ApplyContextualBlockJsonCommand("park.description", "park-1", document.RootElement),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.False(result.Value.IsApplied);
        Assert.False(result.Value.CanApply);
        Assert.Contains(result.Value.Errors, static error => error.Contains("block.name", StringComparison.Ordinal));
        updateParkHandler.Verify(
            handlerMock => handlerMock.HandleAsync(It.IsAny<UpdateParkCommand>(), It.IsAny<CancellationToken>()),
            Times.Never);
        parkRepository.Verify(
            repository => repository.GetByIdAsync("park-1", true, It.IsAny<CancellationToken>()),
            Times.Once);
        parkRepository.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenValidJsonHasNoChange_ShouldNotUpdatePark()
    {
        Park park = CreatePark();
        Mock<IParkRepository> parkRepository = CreateRepository(park);
        Mock<ICommandHandler<UpdateParkCommand, ApplicationResult<Park>>> updateParkHandler = CreateUpdateHandler();
        ApplyContextualBlockJsonCommandHandler handler = CreateHandler(parkRepository, updateParkHandler);

        using JsonDocument document = JsonDocument.Parse(BuildDescriptionDocument("park-1", BuildAllDescriptions()));

        ApplicationResult<ContextualBlockPreviewResult> result = await handler.HandleAsync(
            new ApplyContextualBlockJsonCommand("park.description", "park-1", document.RootElement),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.False(result.Value.IsApplied);
        Assert.True(result.Value.CanApply);
        Assert.Empty(result.Value.Changes);
        updateParkHandler.Verify(
            handlerMock => handlerMock.HandleAsync(It.IsAny<UpdateParkCommand>(), It.IsAny<CancellationToken>()),
            Times.Never);
        parkRepository.Verify(
            repository => repository.GetByIdAsync("park-1", true, It.IsAny<CancellationToken>()),
            Times.Once);
        parkRepository.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenPracticalJsonOmitsCoordinates_ShouldKeepCurrentPosition()
    {
        Park park = CreatePark();
        Mock<IParkRepository> parkRepository = CreateRepository(park);
        Mock<ICommandHandler<UpdateParkCommand, ApplicationResult<Park>>> updateParkHandler = CreateUpdateHandler();
        ApplyContextualBlockJsonCommandHandler handler = CreateHandler(parkRepository, updateParkHandler);

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
                "city": "Lyon"
              }
            }
            """);

        ApplicationResult<ContextualBlockPreviewResult> result = await handler.HandleAsync(
            new ApplyContextualBlockJsonCommand("park.practical", "park-1", document.RootElement),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.True(result.Value.IsApplied);
        Assert.NotNull(park.Position);
        Assert.Equal(48.85, park.Position.Latitude);
        Assert.Equal(2.35, park.Position.Longitude);
        Assert.Equal("Lyon", park.City);
        updateParkHandler.Verify(
            handlerMock => handlerMock.HandleAsync(It.Is<UpdateParkCommand>(command => command.ParkId == "park-1"), It.IsAny<CancellationToken>()),
            Times.Once);
        parkRepository.Verify(
            repository => repository.GetByIdAsync("park-1", true, It.IsAny<CancellationToken>()),
            Times.Exactly(2));
        parkRepository.VerifyAll();
        updateParkHandler.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenPracticalJsonIsValid_ShouldUpdatePracticalFields()
    {
        Park park = CreatePark();
        Mock<IParkRepository> parkRepository = CreateRepository(park);
        Mock<ICommandHandler<UpdateParkCommand, ApplicationResult<Park>>> updateParkHandler = CreateUpdateHandler();
        ApplyContextualBlockJsonCommandHandler handler = CreateHandler(parkRepository, updateParkHandler);

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
                "countryCode": "BE",
                "city": "Bruxelles",
                "street": null,
                "postalCode": "1000",
                "websiteUrl": "https://updated.example.test",
                "founderId": "founder-2",
                "operatorId": "operator-2",
                "latitude": 50.85,
                "longitude": 4.35
              }
            }
            """);

        ApplicationResult<ContextualBlockPreviewResult> result = await handler.HandleAsync(
            new ApplyContextualBlockJsonCommand("park.practical", "park-1", document.RootElement),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.True(result.Value.IsApplied);
        Assert.Equal("BE", park.CountryCode);
        Assert.Equal("Bruxelles", park.City);
        Assert.Null(park.Street);
        Assert.Equal("1000", park.PostalCode);
        Assert.Equal("https://updated.example.test", park.WebsiteUrl);
        Assert.Equal("founder-2", park.FounderId);
        Assert.Equal("operator-2", park.OperatorId);
        Assert.NotNull(park.Position);
        Assert.Equal(50.85, park.Position.Latitude);
        Assert.Equal(4.35, park.Position.Longitude);
        updateParkHandler.Verify(
            handlerMock => handlerMock.HandleAsync(It.Is<UpdateParkCommand>(command => command.ParkId == "park-1"), It.IsAny<CancellationToken>()),
            Times.Once);
        parkRepository.Verify(
            repository => repository.GetByIdAsync("park-1", true, It.IsAny<CancellationToken>()),
            Times.Exactly(2));
        parkRepository.VerifyAll();
        updateParkHandler.VerifyAll();
    }

    private static ApplyContextualBlockJsonCommandHandler CreateHandler(
        Mock<IParkRepository> parkRepository,
        Mock<ICommandHandler<UpdateParkCommand, ApplicationResult<Park>>> updateParkHandler)
    {
        PreviewContextualBlockJsonCommandHandler previewHandler = new PreviewContextualBlockJsonCommandHandler(parkRepository.Object);
        return new ApplyContextualBlockJsonCommandHandler(previewHandler, parkRepository.Object, updateParkHandler.Object);
    }

    private static Mock<ICommandHandler<UpdateParkCommand, ApplicationResult<Park>>> CreateUpdateHandler()
    {
        Mock<ICommandHandler<UpdateParkCommand, ApplicationResult<Park>>> updateParkHandler = new Mock<ICommandHandler<UpdateParkCommand, ApplicationResult<Park>>>(MockBehavior.Strict);
        updateParkHandler
            .Setup(handler => handler.HandleAsync(It.IsAny<UpdateParkCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UpdateParkCommand command, CancellationToken _) => ApplicationResult<Park>.Success(command.Park));
        return updateParkHandler;
    }

    private static Mock<IParkRepository> CreateRepository(Park park)
    {
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        parkRepository
            .Setup(repository => repository.GetByIdAsync("park-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(park);
        return parkRepository;
    }

    private static Park CreatePark()
    {
        Park park = new Park
        {
            Id = "park-1",
            Name = "Apply Park",
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
}
