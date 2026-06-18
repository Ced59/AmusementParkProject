using AmusementPark.Application.Errors;
using AmusementPark.Application.Common.Measurements;
using AmusementPark.Application.Features.AttractionAccessConditionTypes.Ports;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.LocalizedContent.Commands;
using AmusementPark.Application.Features.LocalizedContent.Handlers;
using AmusementPark.Application.Features.LocalizedContent.Results;
using AmusementPark.Application.Features.ParkFounders.Ports;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkOperators.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Application.Features.Search;
using AmusementPark.Application.Features.Search.Ports;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Localization;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.LocalizedContent.Handlers;

public sealed class ApplyLocalizedContentJsonCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenJsonIsInvalid_ShouldReturnInvalidJsonBeforeCallingRepositories()
    {
        HandlerFixture fixture = new HandlerFixture();
        ApplyLocalizedContentJsonCommandHandler handler = fixture.CreateHandler();

        ApplicationResult<LocalizedContentApplyResult> result = await handler.HandleAsync(new ApplyLocalizedContentJsonCommand(
            "park",
            "park-1",
            "not-json"));

        Assert.False(result.IsSuccess);
        ApplicationError error = Assert.Single(result.Errors);
        Assert.Equal("localized-content.json.invalid", error.Code);
        fixture.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenParkItemRawImperialMeasurementsAreProvided_ShouldPersistMetricTruth()
    {
        CancellationToken cancellationToken = new CancellationTokenSource().Token;
        ParkItem existingItem = new ParkItem
        {
            Id = "item-1",
            ParkId = "park-1",
            Name = "American coaster",
            Category = ParkItemCategory.Attraction,
            Type = ParkItemType.RollerCoaster,
            AttractionDetails = new AttractionDetails()
        };
        HandlerFixture fixture = new HandlerFixture();
        fixture.ParkItemRepository
            .Setup(repository => repository.GetByIdAsync("item-1", true, cancellationToken))
            .ReturnsAsync(existingItem);
        fixture.ParkItemRepository
            .Setup(repository => repository.UpdateAsync(
                "item-1",
                It.Is<ParkItem>(item =>
                    item.AttractionDetails != null
                    && item.AttractionDetails.HeightInMeters == 60.96d
                    && item.AttractionDetails.LengthInMeters == 1524d
                    && item.AttractionDetails.SpeedInKmH == 120.7d
                    && item.AttractionDetails.DropInMeters == 54.86d),
                cancellationToken))
            .ReturnsAsync((string itemId, ParkItem item, CancellationToken token) => item);
        fixture.SearchProjectionWriter
            .Setup(writer => writer.UpsertAsync(SearchProjectionResourceTypes.ParkItems, "item-1", cancellationToken))
            .Returns(Task.CompletedTask);
        fixture.SearchProjectionWriter
            .Setup(writer => writer.UpsertAsync(SearchProjectionResourceTypes.Parks, "park-1", cancellationToken))
            .Returns(Task.CompletedTask);
        ApplyLocalizedContentJsonCommandHandler handler = fixture.CreateHandler();
        string json = """
        {
          "heightInFeet": 200,
          "lengthInFeet": 5000,
          "speedInMph": 75,
          "dropInFeet": 180
        }
        """;

        ApplicationResult<LocalizedContentApplyResult> result = await handler.HandleAsync(new ApplyLocalizedContentJsonCommand(
            "park_item",
            "item-1",
            json), cancellationToken);

        Assert.True(result.IsSuccess);
        LocalizedContentApplyResult value = Assert.IsType<LocalizedContentApplyResult>(result.Value);
        Assert.Contains("attractionDetails.heightInMeters", value.UpdatedFields);
        Assert.Contains("attractionDetails.dropInMeters", value.UpdatedFields);
        fixture.VerifyAll();
        fixture.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenParkDescriptionJsonIsValid_ShouldMergeDescriptionsAndRefreshSearchProjections()
    {
        CancellationToken cancellationToken = new CancellationTokenSource().Token;
        Park existingPark = new Park
        {
            Id = "park-1",
            Name = "Bellewaerde",
            Descriptions = new List<LocalizedText>
            {
                new LocalizedText("en", "<p>Old description.</p>")
            }
        };
        ParkItem childItem = new ParkItem
        {
            Id = "item-1",
            ParkId = "park-1",
            Name = "Ride",
            Category = ParkItemCategory.Attraction,
            Type = ParkItemType.RollerCoaster
        };
        HandlerFixture fixture = new HandlerFixture();
        fixture.ParkRepository
            .Setup(repository => repository.GetByIdAsync("park-1", true, cancellationToken))
            .ReturnsAsync(existingPark);
        fixture.ParkRepository
            .Setup(repository => repository.UpdateAsync(
                "park-1",
                It.Is<Park>(park =>
                    park.Descriptions.Count == 2
                    && park.Descriptions.Any(description => description.LanguageCode == "en" && description.Value == "<p>New description.</p>")
                    && park.Descriptions.Any(description => description.LanguageCode == "fr" && description.Value == "<p>Nouvelle description.</p>")),
                cancellationToken))
            .ReturnsAsync((string parkId, Park park, CancellationToken token) => park);
        fixture.ParkItemRepository
            .Setup(repository => repository.GetByParkIdAsync("park-1", true, cancellationToken))
            .ReturnsAsync(new[] { childItem });
        fixture.SearchProjectionWriter
            .Setup(writer => writer.UpsertAsync(SearchProjectionResourceTypes.Parks, "park-1", cancellationToken))
            .Returns(Task.CompletedTask);
        fixture.SearchProjectionWriter
            .Setup(writer => writer.UpsertAsync(SearchProjectionResourceTypes.ParkItems, "item-1", cancellationToken))
            .Returns(Task.CompletedTask);
        ApplyLocalizedContentJsonCommandHandler handler = fixture.CreateHandler();
        string json = """
        {
          "fields": {
            "descriptions": [
              { "languageCode": "fr", "value": "<p>Nouvelle description.</p>" },
              { "languageCode": "en", "value": "<p>New description.</p>" }
            ]
          }
        }
        """;

        ApplicationResult<LocalizedContentApplyResult> result = await handler.HandleAsync(new ApplyLocalizedContentJsonCommand(
            "park",
            "park-1",
            json), cancellationToken);

        Assert.True(result.IsSuccess);
        LocalizedContentApplyResult value = Assert.IsType<LocalizedContentApplyResult>(result.Value);
        Assert.Equal("park", value.EntityType);
        Assert.Equal("park-1", value.EntityId);
        Assert.Equal(2, value.UpdatedLocalizedValueCount);
        Assert.Equal(new[] { "descriptions" }, value.UpdatedFields);
        fixture.VerifyAll();
        fixture.VerifyNoOtherCalls();
    }

    private sealed class HandlerFixture
    {
        public Mock<IParkRepository> ParkRepository { get; } = new Mock<IParkRepository>(MockBehavior.Strict);
        public Mock<IParkZoneRepository> ParkZoneRepository { get; } = new Mock<IParkZoneRepository>(MockBehavior.Strict);
        public Mock<IParkItemRepository> ParkItemRepository { get; } = new Mock<IParkItemRepository>(MockBehavior.Strict);
        public Mock<IParkOperatorRepository> ParkOperatorRepository { get; } = new Mock<IParkOperatorRepository>(MockBehavior.Strict);
        public Mock<IParkFounderRepository> ParkFounderRepository { get; } = new Mock<IParkFounderRepository>(MockBehavior.Strict);
        public Mock<IAttractionManufacturerRepository> AttractionManufacturerRepository { get; } = new Mock<IAttractionManufacturerRepository>(MockBehavior.Strict);
        public Mock<IImageRepository> ImageRepository { get; } = new Mock<IImageRepository>(MockBehavior.Strict);
        public Mock<IImageTagRepository> ImageTagRepository { get; } = new Mock<IImageTagRepository>(MockBehavior.Strict);
        public Mock<IAttractionAccessConditionTypeDefinitionRepository> AccessConditionTypeDefinitionRepository { get; } = new Mock<IAttractionAccessConditionTypeDefinitionRepository>(MockBehavior.Strict);
        public Mock<ISearchProjectionWriter> SearchProjectionWriter { get; } = new Mock<ISearchProjectionWriter>(MockBehavior.Strict);

        public ApplyLocalizedContentJsonCommandHandler CreateHandler()
        {
            return new ApplyLocalizedContentJsonCommandHandler(
                ParkRepository.Object,
                ParkZoneRepository.Object,
                ParkItemRepository.Object,
                ParkOperatorRepository.Object,
                ParkFounderRepository.Object,
                AttractionManufacturerRepository.Object,
                ImageRepository.Object,
                ImageTagRepository.Object,
                AccessConditionTypeDefinitionRepository.Object,
                SearchProjectionWriter.Object,
                MeasurementConversionService.Instance);
        }

        public void VerifyAll()
        {
            ParkRepository.VerifyAll();
            ParkItemRepository.VerifyAll();
            SearchProjectionWriter.VerifyAll();
        }

        public void VerifyNoOtherCalls()
        {
            ParkRepository.VerifyNoOtherCalls();
            ParkZoneRepository.VerifyNoOtherCalls();
            ParkItemRepository.VerifyNoOtherCalls();
            ParkOperatorRepository.VerifyNoOtherCalls();
            ParkFounderRepository.VerifyNoOtherCalls();
            AttractionManufacturerRepository.VerifyNoOtherCalls();
            ImageRepository.VerifyNoOtherCalls();
            ImageTagRepository.VerifyNoOtherCalls();
            AccessConditionTypeDefinitionRepository.VerifyNoOtherCalls();
            SearchProjectionWriter.VerifyNoOtherCalls();
        }
    }
}
