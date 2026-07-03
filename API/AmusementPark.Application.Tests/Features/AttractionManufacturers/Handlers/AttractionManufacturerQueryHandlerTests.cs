using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.AttractionManufacturers.Handlers;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.AttractionManufacturers.Queries;
using AmusementPark.Application.Features.AttractionManufacturers.Results;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Validation;
using AmusementPark.Core.Domain.Images;
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
        Mock<IImageRepository> imageRepository = new Mock<IImageRepository>(MockBehavior.Strict);
        List<AttractionManufacturer> manufacturers = new List<AttractionManufacturer>
        {
            CreateManufacturer("mack", "Mack Rides"),
            CreateManufacturer("vekoma", "Vekoma"),
        };

        manufacturerRepository
            .Setup(repository => repository.GetPageAsync(2, 12, "ride", false, It.IsAny<CancellationToken>()))
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

        SetupManufacturerImages(imageRepository, new[] { "mack", "vekoma" }, new Dictionary<string, string> { ["mack"] = "logo-mack" });

        GetAttractionManufacturersQueryHandler handler = new GetAttractionManufacturersQueryHandler(
            manufacturerRepository.Object,
            parkItemRepository.Object,
            imageRepository.Object,
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
        Assert.Equal("logo-mack", page.Items.First().MainImageId);

        manufacturerRepository.VerifyAll();
        parkItemRepository.VerifyAll();
        imageRepository.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenPaginationInvalid_ShouldReturnFailureWithoutRepositoryCall()
    {
        Mock<IAttractionManufacturerRepository> manufacturerRepository = new Mock<IAttractionManufacturerRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<IImageRepository> imageRepository = new Mock<IImageRepository>(MockBehavior.Strict);
        GetAttractionManufacturersQueryHandler handler = new GetAttractionManufacturersQueryHandler(
            manufacturerRepository.Object,
            parkItemRepository.Object,
            imageRepository.Object,
            new PagedQueryValidator());

        ApplicationResult<PagedResult<AttractionManufacturerResult>> result = await handler.HandleAsync(
            new GetAttractionManufacturersQuery(new PagedQuery(0, 12)));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error => error.Code == "validation.pagination.invalid");
        manufacturerRepository.VerifyNoOtherCalls();
        parkItemRepository.VerifyNoOtherCalls();
        imageRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenStoredCurrentLogoIsNotPublished_ShouldUsePublishedLogoOnList()
    {
        Mock<IAttractionManufacturerRepository> manufacturerRepository = new Mock<IAttractionManufacturerRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<IImageRepository> imageRepository = new Mock<IImageRepository>(MockBehavior.Strict);
        AttractionManufacturer manufacturer = CreateManufacturer("mack", "Mack Rides");
        manufacturer.CurrentLogoImageId = "hidden-logo";

        manufacturerRepository
            .Setup(repository => repository.GetPageAsync(1, 12, null, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<AttractionManufacturer>(new[] { manufacturer }, 1, 12, 1));

        parkItemRepository
            .Setup(repository => repository.GetAttractionCountsByManufacturerIdsAsync(
                It.Is<IEnumerable<string>>(ids => ids.SequenceEqual(new[] { "mack" })),
                It.IsAny<CancellationToken>(),
                false))
            .ReturnsAsync(new Dictionary<string, int>());

        SetupManufacturerImages(imageRepository, new[] { "mack" }, new Dictionary<string, string> { ["mack"] = "published-logo" });

        GetAttractionManufacturersQueryHandler handler = new GetAttractionManufacturersQueryHandler(
            manufacturerRepository.Object,
            parkItemRepository.Object,
            imageRepository.Object,
            new PagedQueryValidator());

        ApplicationResult<PagedResult<AttractionManufacturerResult>> result = await handler.HandleAsync(new GetAttractionManufacturersQuery(new PagedQuery(1, 12)));

        Assert.True(result.IsSuccess);
        AttractionManufacturerResult item = Assert.Single(result.Value!.Items);
        Assert.Equal("published-logo", item.CurrentLogoImageId);
        Assert.Equal("published-logo", item.MainImageId);
        manufacturerRepository.VerifyAll();
        parkItemRepository.VerifyAll();
        imageRepository.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenManufacturerFound_ShouldUseVisibleAttractionCount()
    {
        Mock<IAttractionManufacturerRepository> manufacturerRepository = new Mock<IAttractionManufacturerRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<IImageRepository> imageRepository = new Mock<IImageRepository>(MockBehavior.Strict);

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

        SetupManufacturerImages(imageRepository, new[] { "mack" }, new Dictionary<string, string>(), new Dictionary<string, string> { ["mack"] = "manufacturer-photo" });

        GetAttractionManufacturerByIdQueryHandler handler = new GetAttractionManufacturerByIdQueryHandler(
            manufacturerRepository.Object,
            parkItemRepository.Object,
            imageRepository.Object);

        ApplicationResult<AttractionManufacturerResult> result = await handler.HandleAsync(new GetAttractionManufacturerByIdQuery("mack"));

        Assert.True(result.IsSuccess);
        Assert.Equal(7, result.Value?.AttractionCount);
        Assert.Equal("manufacturer-photo", result.Value?.MainImageId);
        manufacturerRepository.VerifyAll();
        parkItemRepository.VerifyAll();
        imageRepository.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenStoredCurrentLogoIsNotPublished_ShouldFallbackToPublishedManufacturerImageOnDetail()
    {
        Mock<IAttractionManufacturerRepository> manufacturerRepository = new Mock<IAttractionManufacturerRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<IImageRepository> imageRepository = new Mock<IImageRepository>(MockBehavior.Strict);
        AttractionManufacturer manufacturer = CreateManufacturer("mack", "Mack Rides");
        manufacturer.CurrentLogoImageId = "hidden-logo";

        manufacturerRepository
            .Setup(repository => repository.GetByIdAsync("mack", It.IsAny<CancellationToken>()))
            .ReturnsAsync(manufacturer);

        parkItemRepository
            .Setup(repository => repository.GetAttractionCountsByManufacturerIdsAsync(
                It.Is<IEnumerable<string>>(ids => ids.SequenceEqual(new[] { "mack" })),
                It.IsAny<CancellationToken>(),
                false))
            .ReturnsAsync(new Dictionary<string, int>());

        SetupManufacturerImages(imageRepository, new[] { "mack" }, new Dictionary<string, string>(), new Dictionary<string, string> { ["mack"] = "manufacturer-photo" });

        GetAttractionManufacturerByIdQueryHandler handler = new GetAttractionManufacturerByIdQueryHandler(
            manufacturerRepository.Object,
            parkItemRepository.Object,
            imageRepository.Object);

        ApplicationResult<AttractionManufacturerResult> result = await handler.HandleAsync(new GetAttractionManufacturerByIdQuery("mack"));

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value!.CurrentLogoImageId);
        Assert.Equal("manufacturer-photo", result.Value.MainImageId);
        manufacturerRepository.VerifyAll();
        parkItemRepository.VerifyAll();
        imageRepository.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenListIncludesHidden_ShouldForwardIncludeHiddenToRepository()
    {
        Mock<IAttractionManufacturerRepository> manufacturerRepository = new Mock<IAttractionManufacturerRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<IImageRepository> imageRepository = new Mock<IImageRepository>(MockBehavior.Strict);

        manufacturerRepository
            .Setup(repository => repository.GetPageAsync(1, 12, null, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<AttractionManufacturer>(
                new[] { CreateManufacturer("hidden", "Hidden Manufacturer", false) },
                1,
                12,
                1));

        parkItemRepository
            .Setup(repository => repository.GetAttractionCountsByManufacturerIdsAsync(
                It.Is<IEnumerable<string>>(ids => ids.SequenceEqual(new[] { "hidden" })),
                It.IsAny<CancellationToken>(),
                false))
            .ReturnsAsync(new Dictionary<string, int>());

        SetupManufacturerImages(imageRepository, new[] { "hidden" });

        GetAttractionManufacturersQueryHandler handler = new GetAttractionManufacturersQueryHandler(
            manufacturerRepository.Object,
            parkItemRepository.Object,
            imageRepository.Object,
            new PagedQueryValidator());

        ApplicationResult<PagedResult<AttractionManufacturerResult>> result = await handler.HandleAsync(
            new GetAttractionManufacturersQuery(new PagedQuery(1, 12), IncludeHidden: true));

        Assert.True(result.IsSuccess);
        AttractionManufacturerResult manufacturer = Assert.Single(Assert.IsType<PagedResult<AttractionManufacturerResult>>(result.Value).Items);
        Assert.False(manufacturer.IsVisible);
        manufacturerRepository.VerifyAll();
        parkItemRepository.VerifyAll();
        imageRepository.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenManufacturerHiddenAndNotIncluded_ShouldReturnNotFound()
    {
        Mock<IAttractionManufacturerRepository> manufacturerRepository = new Mock<IAttractionManufacturerRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<IImageRepository> imageRepository = new Mock<IImageRepository>(MockBehavior.Strict);

        manufacturerRepository
            .Setup(repository => repository.GetByIdAsync("hidden", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateManufacturer("hidden", "Hidden Manufacturer", false));

        GetAttractionManufacturerByIdQueryHandler handler = new GetAttractionManufacturerByIdQueryHandler(
            manufacturerRepository.Object,
            parkItemRepository.Object,
            imageRepository.Object);

        ApplicationResult<AttractionManufacturerResult> result = await handler.HandleAsync(new GetAttractionManufacturerByIdQuery("hidden"));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error => error.Code == "attraction-manufacturer.not-found");
        manufacturerRepository.VerifyAll();
        parkItemRepository.VerifyNoOtherCalls();
        imageRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenManufacturerHiddenAndIncluded_ShouldReturnManufacturer()
    {
        Mock<IAttractionManufacturerRepository> manufacturerRepository = new Mock<IAttractionManufacturerRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<IImageRepository> imageRepository = new Mock<IImageRepository>(MockBehavior.Strict);

        manufacturerRepository
            .Setup(repository => repository.GetByIdAsync("hidden", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateManufacturer("hidden", "Hidden Manufacturer", false));

        parkItemRepository
            .Setup(repository => repository.GetAttractionCountsByManufacturerIdsAsync(
                It.Is<IEnumerable<string>>(ids => ids.SequenceEqual(new[] { "hidden" })),
                It.IsAny<CancellationToken>(),
                false))
            .ReturnsAsync(new Dictionary<string, int>());

        SetupManufacturerImages(imageRepository, new[] { "hidden" });

        GetAttractionManufacturerByIdQueryHandler handler = new GetAttractionManufacturerByIdQueryHandler(
            manufacturerRepository.Object,
            parkItemRepository.Object,
            imageRepository.Object);

        ApplicationResult<AttractionManufacturerResult> result = await handler.HandleAsync(new GetAttractionManufacturerByIdQuery("hidden", IncludeHidden: true));

        Assert.True(result.IsSuccess);
        Assert.False(result.Value?.IsVisible);
        manufacturerRepository.VerifyAll();
        parkItemRepository.VerifyAll();
        imageRepository.VerifyAll();
    }

    private static AttractionManufacturer CreateManufacturer(string id, string name, bool isVisible = true)
    {
        return new AttractionManufacturer
        {
            Id = id,
            Name = name,
            IsVisible = isVisible,
            AdminReviewStatus = AdminReviewStatus.Validated,
        };
    }

    private static void SetupManufacturerImages(
        Mock<IImageRepository> imageRepository,
        IEnumerable<string> expectedIds,
        IReadOnlyDictionary<string, string>? logoImageIds = null,
        IReadOnlyDictionary<string, string>? manufacturerImageIds = null)
    {
        string[] expectedIdArray = expectedIds.ToArray();
        imageRepository
            .Setup(repository => repository.GetMainImageIdsByOwnersAsync(
                ImageOwnerType.AttractionManufacturer,
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(expectedIdArray)),
                ImageCategory.Logo,
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(logoImageIds ?? new Dictionary<string, string>());

        imageRepository
            .Setup(repository => repository.GetMainImageIdsByOwnersAsync(
                ImageOwnerType.AttractionManufacturer,
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(expectedIdArray)),
                ImageCategory.Manufacturer,
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(manufacturerImageIds ?? new Dictionary<string, string>());
    }
}
