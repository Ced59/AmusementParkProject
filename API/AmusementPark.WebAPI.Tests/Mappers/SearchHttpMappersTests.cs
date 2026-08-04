using AmusementPark.Application.Features.Search.Results;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.WebAPI.Contracts.Parks;
using AmusementPark.WebAPI.Contracts.Searching;
using AmusementPark.WebAPI.Mappers;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Mappers;

public sealed class SearchHttpMappersTests
{
    [Theory]
    [InlineData(ParkStatus.Planned, ParkStatusDto.Planned)]
    [InlineData(ParkStatus.UnderConstruction, ParkStatusDto.UnderConstruction)]
    [InlineData(ParkStatus.Operating, ParkStatusDto.Operating)]
    [InlineData(ParkStatus.TemporarilyClosed, ParkStatusDto.TemporarilyClosed)]
    [InlineData(ParkStatus.ClosedDefinitively, ParkStatusDto.ClosedDefinitively)]
    [InlineData(ParkStatus.Cancelled, ParkStatusDto.Cancelled)]
    public void ToHttp_ShouldExposeParkLifecycleStatus(ParkStatus status, ParkStatusDto expected)
    {
        SearchHitResult result = new SearchHitResult
        {
            Id = "park_1",
            ResourceType = "parks",
            Title = "Lifecycle Park",
            ParkStatus = status,
        };

        SearchResultDto dto = result.ToHttp();

        Assert.Equal(expected, dto.ParkStatus);
    }
}
