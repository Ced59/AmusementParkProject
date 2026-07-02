using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Images.Handlers;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.Images.Queries;
using AmusementPark.Core.Domain.Images;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Images.Handlers;

public sealed class ListImageTagsQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenInactiveTagsAreExcluded_ShouldReturnOnlyActiveTags()
    {
        Mock<IImageTagRepository> imageTagRepository = new Mock<IImageTagRepository>(MockBehavior.Strict);
        imageTagRepository
            .Setup(repository => repository.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new ImageTag { Id = "active", Slug = "active", IsActive = true },
                new ImageTag { Id = "inactive", Slug = "inactive", IsActive = false },
            });
        ListImageTagsQueryHandler handler = new ListImageTagsQueryHandler(imageTagRepository.Object);

        ApplicationResult<IReadOnlyCollection<ImageTag>> result = await handler.HandleAsync(new ListImageTagsQuery(IncludeInactive: false), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        ImageTag tag = Assert.Single(result.Value);
        Assert.Equal("active", tag.Id);
        imageTagRepository.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenInactiveTagsAreIncluded_ShouldReturnAllTags()
    {
        Mock<IImageTagRepository> imageTagRepository = new Mock<IImageTagRepository>(MockBehavior.Strict);
        imageTagRepository
            .Setup(repository => repository.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new ImageTag { Id = "active", Slug = "active", IsActive = true },
                new ImageTag { Id = "inactive", Slug = "inactive", IsActive = false },
            });
        ListImageTagsQueryHandler handler = new ListImageTagsQueryHandler(imageTagRepository.Object);

        ApplicationResult<IReadOnlyCollection<ImageTag>> result = await handler.HandleAsync(new ListImageTagsQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value.Count);
        imageTagRepository.VerifyAll();
    }
}
