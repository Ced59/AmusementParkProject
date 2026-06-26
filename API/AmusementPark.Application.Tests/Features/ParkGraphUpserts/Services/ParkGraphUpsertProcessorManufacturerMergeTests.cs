using System.Text.Json;
using AmusementPark.Application.Common.Contracts;
using AmusementPark.Application.Common.Measurements;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkFounders.Ports;
using AmusementPark.Application.Features.ParkGraphUpserts.Contracts;
using AmusementPark.Application.Features.ParkGraphUpserts.Ports;
using AmusementPark.Application.Features.ParkGraphUpserts.Results;
using AmusementPark.Application.Features.ParkGraphUpserts.Services;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkOperators.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Application.Features.Search;
using AmusementPark.Application.Features.Search.Ports;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.ParkGraphUpserts.Services;

public sealed class ParkGraphUpsertProcessorManufacturerMergeTests
{
    [Fact]
    public async Task ApplyAsync_WhenManufacturerMergeCopiesSourceIdentity_ShouldDeleteSourceBeforeUpdatingTarget()
    {
        AttractionManufacturer source = new AttractionManufacturer
        {
            Id = "manufacturer-source",
            Name = "Anton Schwarzkopf",
            LegalName = "Schwarzkopf GmbH",
        };
        AttractionManufacturer target = new AttractionManufacturer
        {
            Id = "manufacturer-target",
            Name = "Schwarzkopf",
        };
        bool sourceDeletedBeforeTargetUpdate = false;
        AttractionManufacturer? updatedManufacturer = null;

        Mock<IAttractionManufacturerRepository> manufacturerRepository = new Mock<IAttractionManufacturerRepository>(MockBehavior.Strict);
        manufacturerRepository.Setup(value => value.GetByIdAsync("manufacturer-source", It.IsAny<CancellationToken>())).ReturnsAsync(source);
        manufacturerRepository.Setup(value => value.GetByIdAsync("manufacturer-target", It.IsAny<CancellationToken>())).ReturnsAsync(target);
        manufacturerRepository
            .Setup(value => value.DeleteAsync("manufacturer-source", It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((_, _) => sourceDeletedBeforeTargetUpdate = true)
            .ReturnsAsync(true);
        manufacturerRepository
            .Setup(value => value.UpdateAsync("manufacturer-target", It.IsAny<AttractionManufacturer>(), It.IsAny<CancellationToken>()))
            .Callback<string, AttractionManufacturer, CancellationToken>((_, value, _) =>
            {
                Assert.True(sourceDeletedBeforeTargetUpdate);
                updatedManufacturer = value;
            })
            .ReturnsAsync((string _, AttractionManufacturer value, CancellationToken _) => value);

        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        parkItemRepository.Setup(value => value.GetByManufacturerIdAsync("manufacturer-source", true, It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<ParkItem>());

        Mock<IImageRepository> imageRepository = new Mock<IImageRepository>(MockBehavior.Strict);
        imageRepository.Setup(value => value.GetByOwnerAsync(ImageOwnerType.AttractionManufacturer, "manufacturer-source", null, It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<Image>());

        Mock<ISearchProjectionWriter> searchProjectionWriter = new Mock<ISearchProjectionWriter>(MockBehavior.Strict);
        searchProjectionWriter.Setup(value => value.DeleteAsync(SearchProjectionResourceTypes.Manufacturers, "manufacturer-source", It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        searchProjectionWriter.Setup(value => value.UpsertAsync(SearchProjectionResourceTypes.Manufacturers, "manufacturer-target", It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        Mock<IParkGraphUpsertHistoryRepository> historyRepository = new Mock<IParkGraphUpsertHistoryRepository>(MockBehavior.Strict);
        historyRepository.Setup(value => value.SaveAsync(It.IsAny<ParkGraphUpsertHistoryEntry>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        ParkGraphUpsertProcessor processor = new ParkGraphUpsertProcessor(
            Mock.Of<IParkRepository>(MockBehavior.Strict),
            Mock.Of<IParkZoneRepository>(MockBehavior.Strict),
            parkItemRepository.Object,
            Mock.Of<IParkFounderRepository>(MockBehavior.Strict),
            Mock.Of<IParkOperatorRepository>(MockBehavior.Strict),
            manufacturerRepository.Object,
            imageRepository.Object,
            Mock.Of<IRemoteImageImporter>(MockBehavior.Strict),
            searchProjectionWriter.Object,
            historyRepository.Object,
            Mock.Of<IPublicSeoUpdateNotifier>(MockBehavior.Strict),
            MeasurementConversionService.Instance);

        string json = "{\"mode\":\"merge\",\"merges\":[{\"entityType\":\"AttractionManufacturer\",\"sourceId\":\"manufacturer-source\",\"targetId\":\"manufacturer-target\",\"sections\":{\"identity\":\"source\"}}]}";
        using JsonDocument document = JsonDocument.Parse(json);
        ParkGraphUpsertRequest request = new ParkGraphUpsertRequest
        {
            CreateIfMissing = false,
            ReplaceCollections = false,
            Document = document.RootElement.Clone(),
            RawJson = json,
        };

        ApplicationResult<ParkGraphUpsertResult> result = await processor.ApplyAsync(request, "user-1", CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(sourceDeletedBeforeTargetUpdate);
        Assert.NotNull(updatedManufacturer);
        Assert.Equal("Anton Schwarzkopf", updatedManufacturer.Name);
        Assert.Equal("Schwarzkopf GmbH", updatedManufacturer.LegalName);
        manufacturerRepository.VerifyAll();
        parkItemRepository.VerifyAll();
        imageRepository.VerifyAll();
        searchProjectionWriter.VerifyAll();
        historyRepository.VerifyAll();
    }
}
