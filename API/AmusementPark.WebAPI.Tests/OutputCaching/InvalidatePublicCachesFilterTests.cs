using AmusementPark.Application.Ports;
using AmusementPark.WebAPI.OutputCaching;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AmusementPark.WebAPI.Tests.OutputCaching;

public sealed class InvalidatePublicCachesFilterTests
{
    [Fact]
    public async Task OnActionExecutionAsync_WhenSuccessfulMutationWithScopes_ShouldEvictTagsAndInvalidateSsr()
    {
        Mock<IOutputCacheStore> outputCacheStore = new Mock<IOutputCacheStore>(MockBehavior.Strict);
        outputCacheStore
            .Setup(store => store.EvictByTagAsync(ApiOutputCachePolicyNames.PublicDataTag, It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        outputCacheStore
            .Setup(store => store.EvictByTagAsync(ApiOutputCachePolicyNames.PublicReferenceDataTag, It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        Mock<ISsrPageCacheInvalidator> ssrPageCacheInvalidator = new Mock<ISsrPageCacheInvalidator>(MockBehavior.Strict);
        ssrPageCacheInvalidator
            .Setup(invalidator => invalidator.InvalidateAsync(It.IsAny<SsrPageCacheInvalidationRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        Mock<ISsrPageCacheInvalidationRequestResolver> resolver = CreateResolver();
        InvalidatePublicCachesFilter filter = CreateFilter(outputCacheStore, ssrPageCacheInvalidator, resolver);
        ActionExecutingContext context = CreateExecutingContext(
            HttpMethods.Post,
            new InvalidatesPublicCacheAttribute(PublicCacheScope.Data, PublicCacheScope.ReferenceData));

        await filter.OnActionExecutionAsync(context, () => Task.FromResult(CreateExecutedContext(context)));

        outputCacheStore.VerifyAll();
        ssrPageCacheInvalidator.VerifyAll();
    }

    [Fact]
    public async Task OnActionExecutionAsync_WhenOutputCacheEvictionFails_ShouldStillInvalidateSsr()
    {
        Mock<IOutputCacheStore> outputCacheStore = new Mock<IOutputCacheStore>(MockBehavior.Strict);
        outputCacheStore
            .Setup(store => store.EvictByTagAsync(ApiOutputCachePolicyNames.PublicSeoTag, It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromException(new InvalidOperationException("cache backend unavailable")));
        Mock<ISsrPageCacheInvalidator> ssrPageCacheInvalidator = new Mock<ISsrPageCacheInvalidator>(MockBehavior.Strict);
        ssrPageCacheInvalidator
            .Setup(invalidator => invalidator.InvalidateAsync(It.IsAny<SsrPageCacheInvalidationRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        Mock<ISsrPageCacheInvalidationRequestResolver> resolver = CreateResolver();
        InvalidatePublicCachesFilter filter = CreateFilter(outputCacheStore, ssrPageCacheInvalidator, resolver);
        ActionExecutingContext context = CreateExecutingContext(
            HttpMethods.Put,
            new InvalidatesPublicCacheAttribute(PublicCacheScope.Seo));

        await filter.OnActionExecutionAsync(context, () => Task.FromResult(CreateExecutedContext(context)));

        outputCacheStore.VerifyAll();
        ssrPageCacheInvalidator.VerifyAll();
    }

    [Fact]
    public async Task OnActionExecutionAsync_WhenSsrInvalidationFails_ShouldNotThrow()
    {
        Mock<IOutputCacheStore> outputCacheStore = new Mock<IOutputCacheStore>(MockBehavior.Strict);
        outputCacheStore
            .Setup(store => store.EvictByTagAsync(ApiOutputCachePolicyNames.PublicDataTag, It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        Mock<ISsrPageCacheInvalidator> ssrPageCacheInvalidator = new Mock<ISsrPageCacheInvalidator>(MockBehavior.Strict);
        ssrPageCacheInvalidator
            .Setup(invalidator => invalidator.InvalidateAsync(It.IsAny<SsrPageCacheInvalidationRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("ssr unavailable"));
        Mock<ISsrPageCacheInvalidationRequestResolver> resolver = CreateResolver();
        InvalidatePublicCachesFilter filter = CreateFilter(outputCacheStore, ssrPageCacheInvalidator, resolver);
        ActionExecutingContext context = CreateExecutingContext(
            HttpMethods.Delete,
            new InvalidatesPublicCacheAttribute(PublicCacheScope.Data));

        await filter.OnActionExecutionAsync(context, () => Task.FromResult(CreateExecutedContext(context)));

        outputCacheStore.VerifyAll();
        ssrPageCacheInvalidator.VerifyAll();
    }

    [Fact]
    public async Task OnActionExecutionAsync_WhenRequestIsAborted_ShouldUseIndependentInvalidationToken()
    {
        CancellationToken outputCacheCancellationToken = CancellationToken.None;
        CancellationToken ssrCancellationToken = CancellationToken.None;
        Mock<IOutputCacheStore> outputCacheStore = new Mock<IOutputCacheStore>(MockBehavior.Strict);
        outputCacheStore
            .Setup(store => store.EvictByTagAsync(ApiOutputCachePolicyNames.PublicDataTag, It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((_, cancellationToken) => outputCacheCancellationToken = cancellationToken)
            .Returns(ValueTask.CompletedTask);
        Mock<ISsrPageCacheInvalidator> ssrPageCacheInvalidator = new Mock<ISsrPageCacheInvalidator>(MockBehavior.Strict);
        ssrPageCacheInvalidator
            .Setup(invalidator => invalidator.InvalidateAsync(It.IsAny<SsrPageCacheInvalidationRequest>(), It.IsAny<CancellationToken>()))
            .Callback<SsrPageCacheInvalidationRequest, CancellationToken>((_, cancellationToken) => ssrCancellationToken = cancellationToken)
            .Returns(Task.CompletedTask);
        Mock<ISsrPageCacheInvalidationRequestResolver> resolver = CreateResolver();
        InvalidatePublicCachesFilter filter = CreateFilter(outputCacheStore, ssrPageCacheInvalidator, resolver);
        ActionExecutingContext context = CreateExecutingContext(
            HttpMethods.Post,
            new InvalidatesPublicCacheAttribute(PublicCacheScope.Data));
        using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();
        context.HttpContext.RequestAborted = cancellationTokenSource.Token;

        await filter.OnActionExecutionAsync(context, () => Task.FromResult(CreateExecutedContext(context)));

        Assert.False(outputCacheCancellationToken.IsCancellationRequested);
        Assert.False(ssrCancellationToken.IsCancellationRequested);
    }

    private static InvalidatePublicCachesFilter CreateFilter(
        Mock<IOutputCacheStore> outputCacheStore,
        Mock<ISsrPageCacheInvalidator> ssrPageCacheInvalidator,
        Mock<ISsrPageCacheInvalidationRequestResolver> resolver)
    {
        return new InvalidatePublicCachesFilter(
            outputCacheStore.Object,
            ssrPageCacheInvalidator.Object,
            resolver.Object,
            NullLogger<InvalidatePublicCachesFilter>.Instance);
    }

    private static Mock<ISsrPageCacheInvalidationRequestResolver> CreateResolver()
    {
        Mock<ISsrPageCacheInvalidationRequestResolver> resolver = new Mock<ISsrPageCacheInvalidationRequestResolver>(MockBehavior.Strict);
        resolver
            .Setup(value => value.ResolveAsync(
                It.IsAny<ActionExecutingContext>(),
                It.IsAny<ActionExecutedContext?>(),
                It.IsAny<IReadOnlyCollection<PublicCacheScope>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(SsrPageCacheInvalidationRequest.AllCaches());
        return resolver;
    }

    private static ActionExecutingContext CreateExecutingContext(string method, InvalidatesPublicCacheAttribute attribute)
    {
        DefaultHttpContext httpContext = new DefaultHttpContext();
        httpContext.Request.Method = method;
        ActionDescriptor actionDescriptor = new ActionDescriptor
        {
            EndpointMetadata = new List<object> { attribute }
        };
        ActionContext actionContext = new ActionContext(httpContext, new RouteData(), actionDescriptor);

        return new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object?>(),
            new object());
    }

    private static ActionExecutedContext CreateExecutedContext(ActionExecutingContext context)
    {
        return new ActionExecutedContext(
            context,
            new List<IFilterMetadata>(),
            new object())
        {
            Result = new OkResult()
        };
    }
}
