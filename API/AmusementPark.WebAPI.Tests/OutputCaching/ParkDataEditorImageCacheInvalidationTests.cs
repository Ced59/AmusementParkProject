using System.Reflection;
using AmusementPark.Application.Features.Comments.Ports;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Application.Features.StandaloneAttractions.Ports;
using AmusementPark.Application.Ports;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.WebAPI.Contracts.Images;
using AmusementPark.WebAPI.Controllers;
using AmusementPark.WebAPI.OutputCaching;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AmusementPark.WebAPI.Tests.OutputCaching;

public sealed class ParkDataEditorImageCacheInvalidationTests
{
    private static readonly string[] Languages = { "fr", "en", "nl", "de", "es", "it", "pl", "pt" };
    private readonly Mock<IImageRepository> images = new Mock<IImageRepository>(MockBehavior.Strict);
    private readonly Mock<IParkItemRepository> items = new Mock<IParkItemRepository>(MockBehavior.Strict);
    private readonly Mock<IOutputCacheStore> outputCache = new Mock<IOutputCacheStore>(MockBehavior.Strict);
    private readonly Mock<ISsrPageCacheInvalidator> ssrCache = new Mock<ISsrPageCacheInvalidator>(MockBehavior.Strict);
    private SsrPageCacheInvalidationRequest? captured;

    [Fact]
    public async Task UploadWithoutOwner_DoesNotInvalidatePublicCaches()
    {
        await ExecuteAsync(HttpMethods.Post, null, new ParkDataEditorImageCreateDto(), new ImageCreatedDto { Id = "image-1" });

        Assert.Null(this.captured);
        this.outputCache.Verify(store => store.EvictByTagAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        this.ssrCache.Verify(cache => cache.InvalidateAsync(It.IsAny<SsrPageCacheInvalidationRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        this.images.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task LinkToPark_InvalidatesLocalizedParkAndDiscoveryPages()
    {
        SetupPrevious(ImageOwnerType.None, null);

        await ExecuteAsync(HttpMethods.Post, null, LinkRequest(ImageOwnerTypeDto.PARK, "park-1"), Result(ImageOwnerTypeDto.PARK, "park-1"));

        SsrPageCacheInvalidationRequest request = AssertTargeted();
        foreach (string language in Languages)
        {
            Assert.Contains($"/{language}/park/park-1/", request.Prefixes);
            Assert.Contains($"/{language}/parks", request.Paths);
            Assert.Contains($"/{language}/home", request.Paths);
        }

        Assert.Equal(Languages.Length, request.Prefixes.Count);
        Assert.DoesNotContain("/", request.Prefixes);
        AssertOutputEvicted();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task MovingAnImage_InvalidatesPreviousAndNewOwners(bool useLinkRoute)
    {
        SetupPrevious(ImageOwnerType.Park, "old-park");
        object argument = useLinkRoute
            ? LinkRequest(ImageOwnerTypeDto.PARK, "new-park")
            : new UpdateImageAssetRequest { OwnerType = ImageOwnerTypeDto.PARK, OwnerId = "new-park" };

        await ExecuteAsync(useLinkRoute ? HttpMethods.Post : HttpMethods.Put, useLinkRoute ? null : "image-1", argument, Result(ImageOwnerTypeDto.PARK, "new-park"));

        SsrPageCacheInvalidationRequest request = AssertTargeted();
        foreach (string language in Languages)
        {
            Assert.Contains($"/{language}/park/old-park/", request.Prefixes);
            Assert.Contains($"/{language}/park/new-park/", request.Prefixes);
        }

        Assert.Equal(Languages.Length * 2, request.Prefixes.Count);
        this.images.Verify(repository => repository.GetByIdAsync("image-1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetCurrentParkItemImage_InvalidatesParentParkWithoutOtherParks()
    {
        SetupPrevious(ImageOwnerType.ParkItem, "item-1");
        this.items.Setup(repository => repository.GetByIdAsync("item-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ParkItem { Id = "item-1", ParkId = "park-1" });

        await ExecuteAsync(HttpMethods.Put, "image-1", null, Result(ImageOwnerTypeDto.PARK_ITEM, "item-1"));

        SsrPageCacheInvalidationRequest request = AssertTargeted();
        Assert.Empty(request.Paths);
        Assert.Equal(Languages.Select(language => $"/{language}/park/park-1/").Order(), request.Prefixes.Order());
        AssertOutputEvicted();
    }

    [Fact]
    public async Task StandaloneImageMetadata_InvalidatesOnlyLocalizedAttractionPages()
    {
        SetupPrevious(ImageOwnerType.StandaloneAttraction, "standalone-1");

        await ExecuteAsync(HttpMethods.Put, "image-1", new UpdateImageAssetRequest { IsPublished = false }, Result(ImageOwnerTypeDto.STANDALONE_ATTRACTION, "standalone-1"));

        SsrPageCacheInvalidationRequest request = AssertTargeted();
        Assert.Empty(request.Paths);
        Assert.Equal(Languages.Select(language => $"/{language}/attraction/standalone-1/").Order(), request.Prefixes.Order());
        AssertOutputEvicted();
    }

    [Fact]
    public async Task DetachingAnImage_StillInvalidatesThePreviousOwner()
    {
        SetupPrevious(ImageOwnerType.Park, "old-park");

        await ExecuteAsync(HttpMethods.Put, "image-1", new UpdateImageAssetRequest { OwnerType = ImageOwnerTypeDto.NONE }, Result(ImageOwnerTypeDto.NONE, null));

        SsrPageCacheInvalidationRequest request = AssertTargeted();
        Assert.Equal(Languages.Select(language => $"/{language}/park/old-park/").Order(), request.Prefixes.Order());
        AssertOutputEvicted();
    }

    [Fact]
    public async Task MetadataWithoutAnyOwner_DoesNotInvalidatePublicCaches()
    {
        SetupPrevious(ImageOwnerType.None, null);

        await ExecuteAsync(HttpMethods.Put, "image-1", new UpdateImageAssetRequest(), Result(ImageOwnerTypeDto.NONE, null));

        Assert.Null(this.captured);
        this.outputCache.Verify(store => store.EvictByTagAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UnresolvedPreviousOwner_KeepsGlobalFallbackWhenNewOwnerIsKnown(bool missingParent)
    {
        if (missingParent)
        {
            SetupPrevious(ImageOwnerType.ParkItem, "missing-item");
            this.items.Setup(repository => repository.GetByIdAsync("missing-item", true, It.IsAny<CancellationToken>())).ReturnsAsync((ParkItem?)null);
        }
        else
        {
            this.images.Setup(repository => repository.GetByIdAsync("image-1", It.IsAny<CancellationToken>())).ReturnsAsync((Image?)null);
        }

        await ExecuteAsync(HttpMethods.Post, null, LinkRequest(ImageOwnerTypeDto.PARK, "new-park"), Result(ImageOwnerTypeDto.PARK, "new-park"));

        AssertGlobalFallback();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UnresolvedNewParent_KeepsGlobalFallbackWhenPreviousOwnerIsKnown(bool emptyParent)
    {
        SetupPrevious(ImageOwnerType.Park, "old-park");
        this.items.Setup(repository => repository.GetByIdAsync("missing-item", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyParent ? new ParkItem { Id = "missing-item", ParkId = string.Empty } : null);

        await ExecuteAsync(HttpMethods.Put, "image-1", new UpdateImageAssetRequest { OwnerType = ImageOwnerTypeDto.PARK_ITEM, OwnerId = "missing-item" }, Result(ImageOwnerTypeDto.PARK_ITEM, "missing-item"));

        AssertGlobalFallback();
    }

    [Fact]
    public async Task FailedMutation_DoesNotPurgeTheCapturedPreviousOwner()
    {
        SetupPrevious(ImageOwnerType.Park, "park-1");

        await ExecuteAsync(HttpMethods.Put, "image-1", new UpdateImageAssetRequest(), Result(ImageOwnerTypeDto.PARK, "park-1"), StatusCodes.Status400BadRequest);

        Assert.Null(this.captured);
        this.outputCache.Verify(store => store.EvictByTagAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private async Task ExecuteAsync(string method, string? imageId, object? argument, object result, int status = StatusCodes.Status200OK)
    {
        this.outputCache.Setup(store => store.EvictByTagAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(ValueTask.CompletedTask);
        this.ssrCache.Setup(cache => cache.InvalidateAsync(It.IsAny<SsrPageCacheInvalidationRequest>(), It.IsAny<CancellationToken>()))
            .Callback<SsrPageCacheInvalidationRequest, CancellationToken>((request, _) => this.captured = request)
            .Returns(Task.CompletedTask);
        SsrPageCacheInvalidationRequestResolver resolver = new SsrPageCacheInvalidationRequestResolver(
            new Mock<IParkRepository>(MockBehavior.Strict).Object,
            this.items.Object,
            new Mock<ICommentRepository>(MockBehavior.Strict).Object,
            new Mock<IParkZoneRepository>(MockBehavior.Strict).Object,
            this.images.Object,
            new Mock<IStandaloneAttractionRepository>(MockBehavior.Strict).Object);
        InvalidatePublicCachesFilter filter = new InvalidatePublicCachesFilter(
            this.outputCache.Object, this.ssrCache.Object, resolver, NullLogger<InvalidatePublicCachesFilter>.Instance);
        DefaultHttpContext http = new DefaultHttpContext();
        http.Request.Method = method;
        RouteData routes = new RouteData();
        if (imageId is not null)
        {
            routes.Values["imageId"] = imageId;
        }

        ControllerActionDescriptor descriptor = new ControllerActionDescriptor
        {
            ControllerName = nameof(ParkDataEditorImagesController).Replace("Controller", string.Empty, StringComparison.Ordinal),
            EndpointMetadata = typeof(ParkDataEditorImagesController).GetCustomAttributes().Cast<object>().ToList(),
        };
        ActionContext action = new ActionContext(http, routes, descriptor);
        ActionExecutingContext executing = new ActionExecutingContext(
            action, new List<IFilterMetadata>(), new Dictionary<string, object?> { ["request"] = argument }, new object());
        await filter.OnActionExecutionAsync(executing, () => Task.FromResult(new ActionExecutedContext(action, new List<IFilterMetadata>(), new object())
        {
            Result = new ObjectResult(result) { StatusCode = status },
        }));
    }

    private void SetupPrevious(ImageOwnerType ownerType, string? ownerId)
    {
        this.images.Setup(repository => repository.GetByIdAsync("image-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Image { Id = "image-1", OwnerType = ownerType, OwnerId = ownerId });
    }

    private SsrPageCacheInvalidationRequest AssertTargeted()
    {
        SsrPageCacheInvalidationRequest request = Assert.IsType<SsrPageCacheInvalidationRequest>(this.captured);
        Assert.False(request.All);
        Assert.True(request.IncludeSeoDocuments);
        Assert.False(request.AllowStale);
        Assert.False(request.Refresh);
        this.ssrCache.Verify(cache => cache.InvalidateAsync(It.IsAny<SsrPageCacheInvalidationRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        return request;
    }

    private void AssertGlobalFallback()
    {
        Assert.NotNull(this.captured);
        Assert.True(this.captured.All);
        Assert.True(this.captured.IncludeSeoDocuments);
        Assert.False(this.captured.AllowStale);
        Assert.False(this.captured.Refresh);
        AssertOutputEvicted();
    }

    private void AssertOutputEvicted()
    {
        this.outputCache.Verify(store => store.EvictByTagAsync(ApiOutputCachePolicyNames.PublicDataTag, It.IsAny<CancellationToken>()), Times.Once);
        this.outputCache.Verify(store => store.EvictByTagAsync(ApiOutputCachePolicyNames.PublicReferenceDataTag, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static LinkImageToOwnerDto LinkRequest(ImageOwnerTypeDto ownerType, string ownerId)
    {
        return new LinkImageToOwnerDto { ImageId = "image-1", OwnerType = ownerType, OwnerId = ownerId };
    }

    private static ImageDto Result(ImageOwnerTypeDto ownerType, string? ownerId)
    {
        return new ImageDto { Id = "image-1", Category = ImageCategoryDto.PARK, OwnerType = ownerType, OwnerId = ownerId };
    }
}
