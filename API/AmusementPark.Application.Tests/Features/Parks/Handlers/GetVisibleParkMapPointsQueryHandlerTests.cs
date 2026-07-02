using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Countries.Ports;
using AmusementPark.Application.Features.Parks.Contracts;
using AmusementPark.Application.Features.Parks.Handlers;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Parks.Queries;
using AmusementPark.Core.Domain.Parks;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Parks.Handlers;

public sealed class GetVisibleParkMapPointsQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenAudienceClassificationFilterIsProvided_ShouldPassItToRepositoryCriteria()
    {
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<ICountryReferenceService> countryReferenceService = new Mock<ICountryReferenceService>(MockBehavior.Strict);

        countryReferenceService
            .Setup(service => service.FindCountryCodesByLocalizedSearchAsync("legacy", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<string>());
        countryReferenceService
            .Setup(service => service.GetCountryCodesForRegion(null))
            .Returns(Array.Empty<string>());
        parkRepository
            .Setup(repository => repository.GetVisibleMapPointsAsync(
                It.Is<ParkSearchCriteria>(criteria => criteria.AudienceClassificationFilter == ParkAudienceClassificationFilter.Unspecified),
                ClosedEntityFilter.All,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Park>());

        GetVisibleParkMapPointsQueryHandler handler = new GetVisibleParkMapPointsQueryHandler(parkRepository.Object, countryReferenceService.Object);

        ApplicationResult<IReadOnlyCollection<Park>> result = await handler.HandleAsync(
            new GetVisibleParkMapPointsQuery("legacy", null, ParkAudienceClassificationFilter.Unspecified, ClosedEntityFilter.All),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value ?? Array.Empty<Park>());

        parkRepository.VerifyAll();
        countryReferenceService.VerifyAll();
    }
}
