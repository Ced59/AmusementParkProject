using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkItems;
using AmusementPark.Application.Features.ParkItems.Contracts;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Contracts;
using AmusementPark.Application.Features.Parks.Handlers;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Parks.Queries;
using AmusementPark.Application.Features.Parks.Results;
using AmusementPark.Core.Domain.Parks;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Parks.Handlers;

public sealed class GetPublicHomeStatsQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldCountPublicDataWithIndexedRepositoryQueries()
    {
        GetPublicHomeStatsQueryHandlerTestsFakeParkRepository parkRepository = new GetPublicHomeStatsQueryHandlerTestsFakeParkRepository
        {
            ParksCount = 42,
            CountriesCount = 6
        };
        GetPublicHomeStatsQueryHandlerTestsFakeParkItemRepository parkItemRepository = new GetPublicHomeStatsQueryHandlerTestsFakeParkItemRepository
        {
            AttractionCount = 123
        };
        GetPublicHomeStatsQueryHandler handler = new GetPublicHomeStatsQueryHandler(parkRepository, parkItemRepository);

        ApplicationResult<PublicHomeStatsResult> result = await handler.HandleAsync(new GetPublicHomeStatsQuery());

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(42, result.Value.ParksCount);
        Assert.Equal(123, result.Value.AttractionsCount);
        Assert.Equal(6, result.Value.CountriesCount);
        Assert.Equal(new[] { false }, parkRepository.CountIncludeHiddenCalls);
        Assert.Equal(new[] { false }, parkRepository.CountryCountIncludeHiddenCalls);
        Assert.Equal(ParkItemCategory.Attraction, parkItemRepository.CountByCategoryCall?.Category);
        Assert.False(parkItemRepository.CountByCategoryCall?.IncludeHidden);
        Assert.Empty(parkRepository.VisibleParkIdsCalls);
    }






}
