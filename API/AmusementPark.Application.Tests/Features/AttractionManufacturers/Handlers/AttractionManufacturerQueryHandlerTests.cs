using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.AttractionManufacturers.Handlers;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.AttractionManufacturers.Queries;
using AmusementPark.Application.Features.AttractionManufacturers.Results;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Validation;
using AmusementPark.Core.Domain.Parks;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.AttractionManufacturers.Handlers;

public sealed class AttractionManufacturerQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenPageRequested_ShouldLoadRepositoryPageAndVisibleAttractionCounts()
    {
        Mock<IAttractionManufacturerRepository> manufacturerRepository = new Mock<IAttractionManufacturerRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        List<AttractionManufacturer> manufacturers = new List<AttractionManufacturer>
        {
            CreateManufacturer("mack", "Mack Rides"),
            CreateManufacturer("vekoma", "Vekoma"),
        };

        manufacturerRepository
            .Setup(repository => repository.GetPageAsync(2, 12, "ride", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<AttractionManufacturer>(manufacturers, 2, 12, 30));

        parkItemRepository
            .Setup(repository => repository.GetAttractionCountsByManufacturerIdsAsync(
                It.Is<IEnumerable<string>>(ids => ids.SequenceEqual(new[] { "mack", "vekoma" })),
                It.IsAny<CancellationToken>(),
                false))
            .ReturnsAsync(new Dictionary<string, int>
            {
                ["mack"] = 3,
            });

        GetAttractionManufacturersQueryHandler handler = new GetAttractionManufacturersQueryHandler(
            manufacturerRepository.Object,
            parkItemRepository.Object,
            new PagedQueryValidator());

        ApplicationResult<PagedResult<AttractionManufacturerResult>> result = await handler.HandleAsync(
            new GetAttractionManufacturersQuery(new PagedQuery(2, 12), "ride"));

        Assert.True(result.IsSuccess);
        PagedResult<AttractionManufacturerResult> page = Assert.IsType<PagedResult<AttractionManufacturerResult>>(result.Value);
        Assert.Equal(2, page.Page);
        Assert.Equal(12, page.PageSize);
        Assert.Equal(30, page.TotalItems);
        Assert.Equal(new[] { "Mack Rides", "Vekoma" }, page.Items.Select(static item => item.Name).ToArray());
        Assert.Equal(new[] { 3, 0 }, page.Items.Select(static item => item.AttractionCount).ToArray());

        manufacturerRepository.VerifyAll();
        parkItemRepository.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenPaginationInvalid_ShouldReturnFailureWithoutRepositoryCall()
    {
        Mock<IAttractionManufacturerRepository> manufacturerRepository = new Mock<IAttractionManufacturerRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        GetAttractionManufacturersQueryHandler handler = new GetAttractionManufacturersQueryHandler(
            manufacturerRepository.Object,
            parkItemRepository.Object,
            new PagedQueryValidator());

        ApplicationResult<PagedResult<AttractionManufacturerResult>> result = await handler.HandleAsync(
            new GetAttractionManufacturersQuery(new PagedQuery(0, 12)));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error => error.Code == "validation.pagination.invalid");
        manufacturerRepository.VerifyNoOtherCalls();
        parkItemRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenManufacturerFound_ShouldUseVisibleAttractionCount()
    {
        Mock<IAttractionManufacturerRepository> manufacturerRepository = new Mock<IAttractionManufacturerRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);

        manufacturerRepository
            .Setup(repository => repository.GetByIdAsync("mack", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateManufacturer("mack", "Mack Rides"));

        parkItemRepository
            .Setup(repository => repository.GetAttractionCountsByManufacturerIdsAsync(
                It.Is<IEnumerable<string>>(ids => ids.SequenceEqual(new[] { "mack" })),
                It.IsAny<CancellationToken>(),
                false))
            .ReturnsAsync(new Dictionary<string, int>
            {
                ["mack"] = 7,
            });

        GetAttractionManufacturerByIdQueryHandler handler = new GetAttractionManufacturerByIdQueryHandler(
            manufacturerRepository.Object,
            parkItemRepository.Object);

        ApplicationResult<AttractionManufacturerResult> result = await handler.HandleAsync(new GetAttractionManufacturerByIdQuery("mack"));

        Assert.True(result.IsSuccess);
        Assert.Equal(7, result.Value?.AttractionCount);
        manufacturerRepository.VerifyAll();
        parkItemRepository.VerifyAll();
    }

    private static AttractionManufacturer CreateManufacturer(string id, string name)
    {
        return new AttractionManufacturer
        {
            Id = id,
            Name = name,
            AdminReviewStatus = AdminReviewStatus.Validated,
        };
    }
}
