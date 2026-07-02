using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.WebAPI.Contracts.ContextualBlocks;
using AmusementPark.WebAPI.Contracts.ParkGraphUpserts;
using AmusementPark.WebAPI.OutputCaching;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Moq;
using Xunit;

namespace AmusementPark.WebAPI.Tests.OutputCaching;

public sealed class SsrPageCacheInvalidationRequestResolverTests
{
    [Fact]
    public async Task ResolveAsync_ForParkUpdate_ShouldTargetParkAndDiscoveryPages()
    {
        SsrPageCacheInvalidationRequestResolver resolver = CreateResolver();
        ActionExecutingContext context = CreateContext("Parks", new Dictionary<string, object?> { ["id"] = "park-1" });

        AmusementPark.Application.Ports.SsrPageCacheInvalidationRequest request = await resolver.ResolveAsync(
            context,
            null,
            new[] { PublicCacheScope.Data },
            CancellationToken.None);

        Assert.False(request.All);
        Assert.Contains("/fr/park/park-1/", request.Prefixes);
        Assert.Contains("/fr/home", request.Paths);
        Assert.True(request.IncludeSeoDocuments);
    }

    [Fact]
    public async Task ResolveAsync_ForParkItemDeleteBeforeExecution_ShouldTargetParentPark()
    {
        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        parkItemRepository
            .Setup(repository => repository.GetByIdsAsync(
                It.Is<IReadOnlyCollection<string>>(ids => ids.Contains("item-1")),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new ParkItem { Id = "item-1", ParkId = "park-1" } });

        SsrPageCacheInvalidationRequestResolver resolver = CreateResolver(parkItemRepository: parkItemRepository);
        ActionExecutingContext context = CreateContext("ParkItems", new Dictionary<string, object?> { ["id"] = "item-1" });

        AmusementPark.Application.Ports.SsrPageCacheInvalidationRequest request = await resolver.ResolveAsync(
            context,
            null,
            new[] { PublicCacheScope.Data },
            CancellationToken.None);

        Assert.False(request.All);
        Assert.Contains("/fr/park/park-1/", request.Prefixes);
        Assert.DoesNotContain("/fr/home", request.Paths);
        parkItemRepository.VerifyAll();
    }

    [Fact]
    public async Task ResolveAsync_ForManufacturerUpdate_ShouldTargetReferenceAndAffectedParks()
    {
        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        parkItemRepository
            .Setup(repository => repository.GetParkIdsByManufacturerIdAsync("manufacturer-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { "park-1", "park-2" });

        SsrPageCacheInvalidationRequestResolver resolver = CreateResolver(parkItemRepository: parkItemRepository);
        ActionExecutingContext context = CreateContext("AttractionManufacturers", new Dictionary<string, object?> { ["id"] = "manufacturer-1" });

        AmusementPark.Application.Ports.SsrPageCacheInvalidationRequest request = await resolver.ResolveAsync(
            context,
            null,
            new[] { PublicCacheScope.ReferenceData, PublicCacheScope.Data },
            CancellationToken.None);

        Assert.False(request.All);
        Assert.Contains("/fr/park-manufacturer/manufacturer-1/", request.Prefixes);
        Assert.Contains("/fr/park/park-1/", request.Prefixes);
        Assert.Contains("/fr/park/park-2/", request.Prefixes);
        parkItemRepository.VerifyAll();
    }

    [Fact]
    public async Task ResolveAsync_ForParkGraphUpsertBeforeExecution_ShouldReturnNoOp()
    {
        SsrPageCacheInvalidationRequestResolver resolver = CreateResolver();
        ActionExecutingContext context = CreateContext("ParkGraphUpserts", new Dictionary<string, object?>());

        AmusementPark.Application.Ports.SsrPageCacheInvalidationRequest request = await resolver.ResolveAsync(
            context,
            null,
            new[] { PublicCacheScope.Data },
            CancellationToken.None);

        Assert.False(request.All);
        Assert.Empty(request.Paths);
        Assert.Empty(request.Prefixes);
        Assert.False(request.IncludeSeoDocuments);
    }

    [Fact]
    public async Task ResolveAsync_ForContextualBlockParkApply_ShouldTargetParkAndDiscoveryPages()
    {
        SsrPageCacheInvalidationRequestResolver resolver = CreateResolver();
        ActionExecutingContext context = CreateContext(
            "ContextualBlocks",
            new Dictionary<string, object?>
            {
                ["blockType"] = "park.description",
                ["entityId"] = "park-1",
            });
        ActionExecutedContext executedContext = CreateExecutedContext(context, new ContextualBlockPreviewResultDto
        {
            BlockType = "park.description",
            IsApplied = true,
            CanApply = true,
            Target = new ContextualBlockPreviewTargetDto
            {
                EntityType = "Park",
                EntityId = "park-1",
                DisplayName = "Target Park",
            },
            Changes = new List<ContextualBlockPreviewChangeDto>
            {
                new ContextualBlockPreviewChangeDto
                {
                    EntityType = "Park",
                    EntityId = "park-1",
                    Field = "descriptions.fr.value",
                    ChangeType = "Updated",
                },
            },
        });

        AmusementPark.Application.Ports.SsrPageCacheInvalidationRequest request = await resolver.ResolveAsync(
            context,
            executedContext,
            new[] { PublicCacheScope.Data },
            CancellationToken.None);

        Assert.False(request.All);
        Assert.Contains("/fr/park/park-1/", request.Prefixes);
        Assert.Contains("/fr/home", request.Paths);
        Assert.True(request.IncludeSeoDocuments);
    }

    [Fact]
    public async Task ResolveAsync_ForContextualBlockParkItemApply_ShouldTargetImpactedItemPages()
    {
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        parkRepository
            .Setup(repository => repository.GetByIdAsync("park-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Park { Id = "park-1", Name = "Target Park" });

        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        parkItemRepository
            .Setup(repository => repository.GetByIdAsync("item-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ParkItem { Id = "item-1", ParkId = "park-1", Name = "Wakala", ZoneId = "zone-1" });

        Mock<IParkZoneRepository> parkZoneRepository = new Mock<IParkZoneRepository>(MockBehavior.Strict);
        parkZoneRepository
            .Setup(repository => repository.GetByIdAsync("zone-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ParkZone { Id = "zone-1", ParkId = "park-1", Name = "Africa" });

        SsrPageCacheInvalidationRequestResolver resolver = CreateResolver(
            parkRepository: parkRepository,
            parkItemRepository: parkItemRepository,
            parkZoneRepository: parkZoneRepository);
        ActionExecutingContext context = CreateContext(
            "ContextualBlocks",
            new Dictionary<string, object?>
            {
                ["blockType"] = "parkItem.description",
                ["entityId"] = "item-1",
            });
        ActionExecutedContext executedContext = CreateExecutedContext(context, new ContextualBlockPreviewResultDto
        {
            BlockType = "parkItem.description",
            IsApplied = true,
            CanApply = true,
            Target = new ContextualBlockPreviewTargetDto
            {
                EntityType = "ParkItem",
                EntityId = "item-1",
                DisplayName = "Wakala",
            },
            Changes = new List<ContextualBlockPreviewChangeDto>
            {
                new ContextualBlockPreviewChangeDto
                {
                    EntityType = "ParkItem",
                    EntityId = "item-1",
                    Field = "descriptions.fr.value",
                    ChangeType = "Updated",
                },
            },
        });

        AmusementPark.Application.Ports.SsrPageCacheInvalidationRequest request = await resolver.ResolveAsync(
            context,
            executedContext,
            new[] { PublicCacheScope.Data },
            CancellationToken.None);

        Assert.False(request.All);
        Assert.Contains("/fr/park/park-1/target-park", request.Paths);
        Assert.Contains("/fr/park/park-1/target-park/items", request.Paths);
        Assert.Contains("/fr/park/park-1/target-park/zones", request.Paths);
        Assert.Contains("/fr/park/park-1/target-park/item/item-1/", request.Prefixes);
        Assert.DoesNotContain("/fr/home", request.Paths);
        parkRepository.VerifyAll();
        parkItemRepository.VerifyAll();
        parkZoneRepository.VerifyAll();
    }

    [Fact]
    public async Task ResolveAsync_ForContextualBlockRejectedApply_ShouldReturnNoOp()
    {
        SsrPageCacheInvalidationRequestResolver resolver = CreateResolver();
        ActionExecutingContext context = CreateContext(
            "ContextualBlocks",
            new Dictionary<string, object?>
            {
                ["blockType"] = "park.description",
                ["entityId"] = "park-1",
            });
        ActionExecutedContext executedContext = CreateExecutedContext(context, new ContextualBlockPreviewResultDto
        {
            BlockType = "park.description",
            IsApplied = false,
            CanApply = false,
            Target = new ContextualBlockPreviewTargetDto
            {
                EntityType = "Park",
                EntityId = "park-1",
                DisplayName = "Target Park",
            },
        });

        AmusementPark.Application.Ports.SsrPageCacheInvalidationRequest request = await resolver.ResolveAsync(
            context,
            executedContext,
            new[] { PublicCacheScope.Data },
            CancellationToken.None);

        Assert.False(request.All);
        Assert.Empty(request.Paths);
        Assert.Empty(request.Prefixes);
        Assert.False(request.IncludeSeoDocuments);
    }

    [Fact]
    public async Task ResolveAsync_ForParkGraphUpsertWithoutChangedEntity_ShouldReturnNoOp()
    {
        SsrPageCacheInvalidationRequestResolver resolver = CreateResolver();
        ActionExecutingContext context = CreateContext("ParkGraphUpserts", new Dictionary<string, object?>());
        ActionExecutedContext executedContext = CreateExecutedContext(context, new ParkGraphUpsertResultDto
        {
            TargetParkId = "park-1",
            Changes = new List<ParkGraphUpsertChangeDto>
            {
                new ParkGraphUpsertChangeDto
                {
                    EntityType = "ParkItem",
                    EntityId = "item-1",
                    ChangeType = "Unchanged",
                },
            },
        });

        AmusementPark.Application.Ports.SsrPageCacheInvalidationRequest request = await resolver.ResolveAsync(
            context,
            executedContext,
            new[] { PublicCacheScope.Data },
            CancellationToken.None);

        Assert.False(request.All);
        Assert.Empty(request.Paths);
        Assert.Empty(request.Prefixes);
        Assert.False(request.IncludeSeoDocuments);
    }

    [Fact]
    public async Task ResolveAsync_ForParkGraphUpsertItemChange_ShouldTargetOnlyImpactedItemPages()
    {
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        parkRepository
            .Setup(repository => repository.GetByIdAsync("park-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Park { Id = "park-1", Name = "Target Park" });

        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        parkItemRepository
            .Setup(repository => repository.GetByIdAsync("item-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ParkItem { Id = "item-1", ParkId = "park-1", Name = "Wakala", ZoneId = "zone-1" });

        Mock<IParkZoneRepository> parkZoneRepository = new Mock<IParkZoneRepository>(MockBehavior.Strict);
        parkZoneRepository
            .Setup(repository => repository.GetByIdAsync("zone-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ParkZone { Id = "zone-1", ParkId = "park-1", Name = "Africa" });

        SsrPageCacheInvalidationRequestResolver resolver = CreateResolver(
            parkRepository: parkRepository,
            parkItemRepository: parkItemRepository,
            parkZoneRepository: parkZoneRepository);
        ActionExecutingContext context = CreateContext("ParkGraphUpserts", new Dictionary<string, object?>());
        ActionExecutedContext executedContext = CreateExecutedContext(context, new ParkGraphUpsertResultDto
        {
            TargetParkId = "park-1",
            Changes = new List<ParkGraphUpsertChangeDto>
            {
                new ParkGraphUpsertChangeDto
                {
                    EntityType = "ParkItem",
                    EntityId = "item-1",
                    ChangeType = "Updated",
                    Fields = new List<ParkGraphUpsertFieldChangeDto>
                    {
                        new ParkGraphUpsertFieldChangeDto { Field = "descriptions.fr", OldValue = "Old", NewValue = "New" },
                    },
                },
            },
        });

        AmusementPark.Application.Ports.SsrPageCacheInvalidationRequest request = await resolver.ResolveAsync(
            context,
            executedContext,
            new[] { PublicCacheScope.Data },
            CancellationToken.None);

        Assert.False(request.All);
        Assert.Contains("/fr/park/park-1/target-park", request.Paths);
        Assert.Contains("/fr/park/park-1/target-park/items", request.Paths);
        Assert.Contains("/fr/park/park-1/target-park/zones", request.Paths);
        Assert.Contains("/fr/park/park-1/target-park/item/item-1/", request.Prefixes);
        Assert.Contains("/fr/park/park-1/target-park/zone/zone-1/", request.Prefixes);
        Assert.DoesNotContain("/fr/park/park-1/", request.Prefixes);
        Assert.DoesNotContain("/fr/home", request.Paths);
        Assert.False(request.IncludeSeoDocuments);
        Assert.False(request.Refresh);
        parkRepository.VerifyAll();
        parkItemRepository.VerifyAll();
        parkZoneRepository.VerifyAll();
    }

    [Fact]
    public async Task ResolveAsync_ForParkGraphUpsertParkChange_ShouldTargetParkAndDiscoveryPages()
    {
        SsrPageCacheInvalidationRequestResolver resolver = CreateResolver();
        ActionExecutingContext context = CreateContext("ParkGraphUpserts", new Dictionary<string, object?>());
        ActionExecutedContext executedContext = CreateExecutedContext(context, new ParkGraphUpsertResultDto
        {
            TargetParkId = "park-1",
            Changes = new List<ParkGraphUpsertChangeDto>
            {
                new ParkGraphUpsertChangeDto
                {
                    EntityType = "Park",
                    EntityId = "park-1",
                    ChangeType = "Updated",
                    Fields = new List<ParkGraphUpsertFieldChangeDto>
                    {
                        new ParkGraphUpsertFieldChangeDto { Field = "isVisible", OldValue = "true", NewValue = "false" },
                    },
                },
            },
        });

        AmusementPark.Application.Ports.SsrPageCacheInvalidationRequest request = await resolver.ResolveAsync(
            context,
            executedContext,
            new[] { PublicCacheScope.Data },
            CancellationToken.None);

        Assert.False(request.All);
        Assert.Contains("/fr/park/park-1/", request.Prefixes);
        Assert.Contains("/fr/home", request.Paths);
        Assert.False(request.AllowStale);
        Assert.False(request.Refresh);
        Assert.False(request.IncludeSeoDocuments);
    }

    [Fact]
    public async Task ResolveAsync_ForLargeParkGraphUpsert_ShouldTargetParkWithoutSeoDocumentsOrHardPurge()
    {
        SsrPageCacheInvalidationRequestResolver resolver = CreateResolver();
        ActionExecutingContext context = CreateContext("ParkGraphUpserts", new Dictionary<string, object?>());
        List<ParkGraphUpsertChangeDto> changes = Enumerable.Range(1, 101)
            .Select(index => new ParkGraphUpsertChangeDto
            {
                EntityType = "ParkItem",
                EntityId = $"item-{index}",
                ChangeType = "Updated",
            })
            .ToList();
        ActionExecutedContext executedContext = CreateExecutedContext(context, new ParkGraphUpsertResultDto
        {
            TargetParkId = "park-1",
            Changes = changes,
        });

        AmusementPark.Application.Ports.SsrPageCacheInvalidationRequest request = await resolver.ResolveAsync(
            context,
            executedContext,
            new[] { PublicCacheScope.Data },
            CancellationToken.None);

        Assert.False(request.All);
        Assert.Contains("/fr/park/park-1/", request.Prefixes);
        Assert.Contains("/fr/home", request.Paths);
        Assert.False(request.IncludeSeoDocuments);
        Assert.True(request.AllowStale);
        Assert.False(request.Refresh);
    }

    [Fact]
    public async Task ResolveAsync_ForLargeParkGraphUpsertWithVisibilityRemoval_ShouldForceHardPurge()
    {
        SsrPageCacheInvalidationRequestResolver resolver = CreateResolver();
        ActionExecutingContext context = CreateContext("ParkGraphUpserts", new Dictionary<string, object?>());
        List<ParkGraphUpsertChangeDto> changes = Enumerable.Range(1, 101)
            .Select(index => new ParkGraphUpsertChangeDto
            {
                EntityType = "ParkItem",
                EntityId = $"item-{index}",
                ChangeType = "Updated",
                Fields = index == 1
                    ? new List<ParkGraphUpsertFieldChangeDto>
                    {
                        new ParkGraphUpsertFieldChangeDto { Field = "isVisible", OldValue = "true", NewValue = "false" },
                    }
                    : new List<ParkGraphUpsertFieldChangeDto>(),
            })
            .ToList();
        ActionExecutedContext executedContext = CreateExecutedContext(context, new ParkGraphUpsertResultDto
        {
            TargetParkId = "park-1",
            Changes = changes,
        });

        AmusementPark.Application.Ports.SsrPageCacheInvalidationRequest request = await resolver.ResolveAsync(
            context,
            executedContext,
            new[] { PublicCacheScope.Data },
            CancellationToken.None);

        Assert.False(request.All);
        Assert.Contains("/fr/park/park-1/", request.Prefixes);
        Assert.False(request.AllowStale);
        Assert.False(request.Refresh);
    }

    [Fact]
    public async Task ResolveAsync_ForLargeParkGraphUpsertWithDeletedEntities_ShouldForceHardPurge()
    {
        SsrPageCacheInvalidationRequestResolver resolver = CreateResolver();
        ActionExecutingContext context = CreateContext("ParkGraphUpserts", new Dictionary<string, object?>());
        List<ParkGraphUpsertChangeDto> changes = Enumerable.Range(1, 101)
            .Select(index => new ParkGraphUpsertChangeDto
            {
                EntityType = "ParkItem",
                EntityId = $"item-{index}",
                ChangeType = "Deleted",
            })
            .ToList();
        ActionExecutedContext executedContext = CreateExecutedContext(context, new ParkGraphUpsertResultDto
        {
            TargetParkId = "park-1",
            Changes = changes,
        });

        AmusementPark.Application.Ports.SsrPageCacheInvalidationRequest request = await resolver.ResolveAsync(
            context,
            executedContext,
            new[] { PublicCacheScope.Data },
            CancellationToken.None);

        Assert.False(request.All);
        Assert.Contains("/fr/park/park-1/", request.Prefixes);
        Assert.False(request.AllowStale);
        Assert.False(request.Refresh);
    }

    [Fact]
    public async Task ResolveAsync_ForParkGraphUpsertImageOwnerChange_ShouldTargetOldAndNewOwners()
    {
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        parkRepository
            .Setup(repository => repository.GetByIdAsync("park-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Park { Id = "park-1", Name = "Target Park" });

        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        parkItemRepository
            .Setup(repository => repository.GetByIdAsync("item-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ParkItem { Id = "item-1", ParkId = "park-1", Name = "Old Owner" });
        parkItemRepository
            .Setup(repository => repository.GetByIdAsync("item-2", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ParkItem { Id = "item-2", ParkId = "park-1", Name = "New Owner" });

        Mock<IImageRepository> imageRepository = new Mock<IImageRepository>(MockBehavior.Strict);
        imageRepository
            .Setup(repository => repository.GetByIdAsync("image-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Image { Id = "image-1", OwnerType = ImageOwnerType.ParkItem, OwnerId = "item-2" });

        SsrPageCacheInvalidationRequestResolver resolver = CreateResolver(
            parkRepository: parkRepository,
            parkItemRepository: parkItemRepository,
            imageRepository: imageRepository);
        ActionExecutingContext context = CreateContext("ParkGraphUpserts", new Dictionary<string, object?>());
        ActionExecutedContext executedContext = CreateExecutedContext(context, new ParkGraphUpsertResultDto
        {
            TargetParkId = "park-1",
            Changes = new List<ParkGraphUpsertChangeDto>
            {
                new ParkGraphUpsertChangeDto
                {
                    EntityType = "Image",
                    EntityId = "image-1",
                    ChangeType = "Updated",
                    Fields = new List<ParkGraphUpsertFieldChangeDto>
                    {
                        new ParkGraphUpsertFieldChangeDto { Field = "ownerId", OldValue = "item-1", NewValue = "item-2" },
                    },
                },
            },
        });

        AmusementPark.Application.Ports.SsrPageCacheInvalidationRequest request = await resolver.ResolveAsync(
            context,
            executedContext,
            new[] { PublicCacheScope.Data },
            CancellationToken.None);

        Assert.False(request.All);
        Assert.Contains("/fr/park/park-1/target-park/item/item-1/", request.Prefixes);
        Assert.Contains("/fr/park/park-1/target-park/item/item-2/", request.Prefixes);
        Assert.DoesNotContain("/fr/park/park-1/", request.Prefixes);
        Assert.False(request.IncludeSeoDocuments);
        Assert.False(request.Refresh);
        parkRepository.Verify(repository => repository.GetByIdAsync("park-1", true, It.IsAny<CancellationToken>()), Times.Exactly(2));
        parkItemRepository.VerifyAll();
        imageRepository.VerifyAll();
    }

    [Fact]
    public async Task ResolveAsync_ForImageMutation_ShouldTargetOwnerWithoutImmediateRefresh()
    {
        SsrPageCacheInvalidationRequestResolver resolver = CreateResolver();
        ActionExecutingContext context = CreateContext("Images", new Dictionary<string, object?>());
        ActionExecutedContext executedContext = CreateExecutedContext(context, new Image
        {
            Id = "image-1",
            OwnerType = ImageOwnerType.Park,
            OwnerId = "park-1",
        });

        AmusementPark.Application.Ports.SsrPageCacheInvalidationRequest request = await resolver.ResolveAsync(
            context,
            executedContext,
            new[] { PublicCacheScope.Data },
            CancellationToken.None);

        Assert.False(request.All);
        Assert.Contains("/fr/park/park-1/", request.Prefixes);
        Assert.Contains("/fr/home", request.Paths);
        Assert.True(request.IncludeSeoDocuments);
        Assert.True(request.AllowStale);
        Assert.False(request.Refresh);
    }

    private static SsrPageCacheInvalidationRequestResolver CreateResolver(
        Mock<IParkRepository>? parkRepository = null,
        Mock<IParkItemRepository>? parkItemRepository = null,
        Mock<IParkZoneRepository>? parkZoneRepository = null,
        Mock<IImageRepository>? imageRepository = null)
    {
        return new SsrPageCacheInvalidationRequestResolver(
            (parkRepository ?? new Mock<IParkRepository>(MockBehavior.Strict)).Object,
            (parkItemRepository ?? new Mock<IParkItemRepository>(MockBehavior.Strict)).Object,
            (parkZoneRepository ?? new Mock<IParkZoneRepository>(MockBehavior.Strict)).Object,
            (imageRepository ?? new Mock<IImageRepository>(MockBehavior.Strict)).Object);
    }

    private static ActionExecutingContext CreateContext(string controllerName, IDictionary<string, object?> routeValues)
    {
        DefaultHttpContext httpContext = new DefaultHttpContext();
        RouteData routeData = new RouteData();

        foreach (KeyValuePair<string, object?> routeValue in routeValues)
        {
            routeData.Values[routeValue.Key] = routeValue.Value;
        }

        ControllerActionDescriptor descriptor = new ControllerActionDescriptor
        {
            ControllerName = controllerName,
        };

        ActionContext actionContext = new ActionContext(httpContext, routeData, descriptor);

        return new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object?>(),
            new object());
    }

    private static ActionExecutedContext CreateExecutedContext(ActionExecutingContext context, object result)
    {
        ActionContext actionContext = new ActionContext(
            context.HttpContext,
            context.RouteData,
            context.ActionDescriptor,
            context.ModelState);

        return new ActionExecutedContext(
            actionContext,
            new List<IFilterMetadata>(),
            new object())
        {
            Result = new OkObjectResult(result),
        };
    }
}
