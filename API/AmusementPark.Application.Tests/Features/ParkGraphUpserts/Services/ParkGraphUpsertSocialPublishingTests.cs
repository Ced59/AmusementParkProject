using System.Text.Json;
using AmusementPark.Application.Common.Measurements;
using AmusementPark.Application.Errors;
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
using AmusementPark.Application.Features.SocialPublishing.Ports;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.SocialPublishing;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.ParkGraphUpserts.Services;

public sealed class ParkGraphUpsertSocialPublishingTests
{
    [Fact]
    public async Task ApplyAsync_WhenParkBecomesPublic_ShouldPublishAfterParkAndSeoUpdates()
    {
        Park park = new Park
        {
            Id = "park-1",
            Name = "Launch Park",
            CountryCode = "FR",
            IsVisible = false,
            Status = ParkStatus.Operating,
            AdminReviewStatus = AdminReviewStatus.Validated,
        };
        List<string> completedSteps = new List<string>();

        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        parkRepository
            .Setup(repository => repository.GetByIdAsync("park-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(park);
        parkRepository
            .Setup(repository => repository.UpdateAsync("park-1", It.IsAny<Park>(), It.IsAny<CancellationToken>()))
            .Callback(() => completedSteps.Add("park"))
            .ReturnsAsync((string _, Park updatedPark, CancellationToken _) => updatedPark);

        Mock<ISearchProjectionWriter> searchProjectionWriter = new Mock<ISearchProjectionWriter>(MockBehavior.Strict);
        searchProjectionWriter
            .Setup(writer => writer.UpsertAsync(SearchProjectionResourceTypes.Parks, "park-1", It.IsAny<CancellationToken>()))
            .Callback(() => completedSteps.Add("search"))
            .Returns(Task.CompletedTask);

        Mock<IPublicSeoUpdateNotifier> seoNotifier = new Mock<IPublicSeoUpdateNotifier>(MockBehavior.Strict);
        seoNotifier
            .Setup(notifier => notifier.NotifyAsync(It.IsAny<AmusementPark.Application.Features.Seo.Models.PublicSeoUpdate>(), It.IsAny<CancellationToken>()))
            .Callback(() => completedSteps.Add("seo"))
            .Returns(Task.CompletedTask);

        Mock<ISocialPublicationService> socialPublicationService = new Mock<ISocialPublicationService>(MockBehavior.Strict);
        socialPublicationService
            .Setup(service => service.PublishParkAnnouncementAsync(
                It.Is<Park>(candidate => candidate.Id == "park-1" && candidate.IsVisible),
                "admin-1",
                It.IsAny<CancellationToken>()))
            .Callback(() => completedSteps.Add("social"))
            .ReturnsAsync(new SocialPublication
            {
                Id = "publication-1",
                Status = SocialPublicationStatus.Published,
            });

        Mock<IParkGraphUpsertHistoryRepository> historyRepository = new Mock<IParkGraphUpsertHistoryRepository>(MockBehavior.Strict);
        historyRepository
            .Setup(repository => repository.SaveAsync(It.IsAny<ParkGraphUpsertHistoryEntry>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        ParkGraphUpsertProcessor processor = new ParkGraphUpsertProcessor(
            parkRepository.Object,
            Mock.Of<IParkZoneRepository>(MockBehavior.Strict),
            Mock.Of<IParkItemRepository>(MockBehavior.Strict),
            Mock.Of<IParkFounderRepository>(MockBehavior.Strict),
            Mock.Of<IParkOperatorRepository>(MockBehavior.Strict),
            Mock.Of<IAttractionManufacturerRepository>(MockBehavior.Strict),
            Mock.Of<IImageRepository>(MockBehavior.Strict),
            Mock.Of<IRemoteImageImporter>(MockBehavior.Strict),
            searchProjectionWriter.Object,
            historyRepository.Object,
            seoNotifier.Object,
            MeasurementConversionService.Instance,
            socialPublicationService: socialPublicationService.Object);

        using JsonDocument document = JsonDocument.Parse("""
        {
          "park": {
            "isVisible": true
          }
        }
        """);
        ParkGraphUpsertRequest request = new ParkGraphUpsertRequest
        {
            TargetParkId = "park-1",
            Document = document.RootElement.Clone(),
            RawJson = document.RootElement.GetRawText(),
        };

        ApplicationResult<ParkGraphUpsertResult> result = await processor.ApplyAsync(
            request,
            "admin-1",
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(new[] { "park", "search", "seo", "social" }, completedSteps);
        parkRepository.VerifyAll();
        searchProjectionWriter.VerifyAll();
        seoNotifier.VerifyAll();
        socialPublicationService.VerifyAll();
        historyRepository.VerifyAll();
    }
}
