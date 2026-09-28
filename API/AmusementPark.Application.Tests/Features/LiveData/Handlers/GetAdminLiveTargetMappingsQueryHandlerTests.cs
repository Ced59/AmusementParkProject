using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.LiveData.Handlers;
using AmusementPark.Application.Features.LiveData.Models;
using AmusementPark.Application.Features.LiveData.Ports;
using AmusementPark.Application.Features.LiveData.Queries;
using AmusementPark.Application.Features.LiveData.Results;
using AmusementPark.Core.Domain.LiveData;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.LiveData.Handlers;

public sealed class GetAdminLiveTargetMappingsQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldRejectOversizedPagesBeforeRepositoryAccess()
    {
        Mock<ILiveTargetMappingRepository> repository = new Mock<ILiveTargetMappingRepository>(
            MockBehavior.Strict);
        GetAdminLiveTargetMappingsQueryHandler handler = new(repository.Object);

        ApplicationResult<PagedResult<LiveTargetMappingResult>> result = await handler.HandleAsync(
            new GetAdminLiveTargetMappingsQuery(
                new LiveTargetMappingSearchCriteria(1, 101, null, null, null, null)),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("live-data.mapping.search.invalid", Assert.Single(result.Errors).Code);
    }

    [Fact]
    public async Task HandleAsync_ShouldMapOnlyLatestRepositoryResults()
    {
        DateTime nowUtc = new DateTime(2026, 9, 28, 17, 0, 0, DateTimeKind.Utc);
        ExternalLiveTargetMapping mapping = ExternalLiveTargetMapping.CreateCandidate(
            Guid.NewGuid(),
            LiveDataSourceId.Parse("themeparks-wiki"),
            new ExternalLiveTargetDescriptor(
                LiveTargetType.Park,
                "external-park-1",
                null,
                "Phantasialand",
                null,
                "DE"),
            null,
            LiveMappingConfidence.Low,
            nowUtc);
        LiveTargetMappingSearchCriteria criteria = new(1, 25, null, null, null, null);
        Mock<ILiveTargetMappingRepository> repository = new Mock<ILiveTargetMappingRepository>(
            MockBehavior.Strict);
        repository.Setup(candidate => candidate.SearchLatestAsync(
                criteria,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<ExternalLiveTargetMapping>(new[] { mapping }, 1, 25, 1));
        GetAdminLiveTargetMappingsQueryHandler handler = new(repository.Object);

        ApplicationResult<PagedResult<LiveTargetMappingResult>> result = await handler.HandleAsync(
            new GetAdminLiveTargetMappingsQuery(criteria),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        LiveTargetMappingResult item = Assert.Single(result.Value!.Items);
        Assert.Equal(mapping.Version, item.Version);
        Assert.Equal("external-park-1", item.ExternalTarget.Id);
        repository.VerifyAll();
    }
}
