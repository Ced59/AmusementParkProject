using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Countries.Ports;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkOpeningHours.Ports;
using AmusementPark.Application.Features.ParkOpeningHours.Services;
using AmusementPark.Application.Features.Parks.Contracts;
using AmusementPark.Application.Features.Parks.Handlers;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Parks.Queries;
using AmusementPark.Application.Features.Parks.Results;
using AmusementPark.Application.Validation;
using AmusementPark.Core.Domain.Parks;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Parks.Handlers;

public sealed class SearchParksQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenAudienceClassificationFilterIsProvided_ShouldPassItToRepositoryCriteria()
    {
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<IParkOpeningHoursRepository> openingHoursRepository = new Mock<IParkOpeningHoursRepository>(MockBehavior.Strict);
        Mock<ICountryReferenceService> countryReferenceService = new Mock<ICountryReferenceService>(MockBehavior.Strict);

        countryReferenceService
            .Setup(service => service.FindCountryCodesByLocalizedSearchAsync("Europa", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { "FR" });
        countryReferenceService
            .Setup(service => service.GetCountryCodesForRegion(null))
            .Returns(Array.Empty<string>());
        parkRepository
            .Setup(repository => repository.SearchAsync(
                It.Is<ParkSearchCriteria>(criteria => criteria.AudienceClassificationFilter == ParkAudienceClassificationFilter.National),
                1,
                12,
                false,
                true,
                null,
                null,
                null,
                null,
                ClosedEntityFilter.OpenOnly,
                It.IsAny<CancellationToken>(),
                ParkAdminSortField.Default,
                false))
            .ReturnsAsync(new PagedResult<Park>(Array.Empty<Park>(), 1, 12, 0));

        SearchParksQueryHandler handler = new SearchParksQueryHandler(
            parkRepository.Object,
            parkItemRepository.Object,
            openingHoursRepository.Object,
            new ParkOpeningHoursAdminStatusResolver(),
            countryReferenceService.Object,
            new PagedQueryValidator());

        ApplicationResult<PagedResult<ParkListResult>> result = await handler.HandleAsync(
            new SearchParksQuery(
                "Europa",
                null,
                new PagedQuery(1, 12),
                IsVisible: true,
                AudienceClassificationFilter: ParkAudienceClassificationFilter.National),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(0, result.Value.TotalItems);

        parkRepository.VerifyAll();
        parkItemRepository.VerifyNoOtherCalls();
        openingHoursRepository.VerifyNoOtherCalls();
        countryReferenceService.VerifyAll();
    }
}
