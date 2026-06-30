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
    public async Task OnActionExecutionAsync_WhenSsrInvalidationIsNoOp_ShouldSkipSsrCall()
    {
        Mock<IOutputCacheStore> outputCacheStore = CreateOutputCacheStore(ApiOutputCachePolicyNames.PublicDataTag);
        Mock<ISsrPageCacheInvalidator> ssrPageCacheInvalidator = new Mock<ISsrPageCacheInvalidator>(MockBehavior.Strict);
        Mock<ISsrPageCacheInvalidationRequestResolver> resolver = CreateResolver(CreateNoOpRequest());
        InvalidatePublicCachesFilter filter = CreateFilter(outputCacheStore, ssrPageCacheInvalidator, resolver);
        ActionExecutingContext context = CreateExecutingContext(
            HttpMethods.Post,
            new InvalidatesPublicCacheAttribute(PublicCacheScope.Data));

        await filter.OnActionExecutionAsync(context, () => Task.FromResult(CreateExecutedContext(context)));

        outputCacheStore.VerifyAll();
        ssrPageCacheInvalidator.Verify(
            invalidator => invalidator.InvalidateAsync(It.IsAny<SsrPageCacheInvalidationRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task OnActionExecutionAsync_WhenOutputCacheEvictionIsDisabled_ShouldOnlyInvalidateSsr()
    {
        Mock<IOutputCacheStore> outputCacheStore = new Mock<IOutputCacheStore>(MockBehavior.Strict);
        Mock<ISsrPageCacheInvalidator> ssrPageCacheInvalidator = new Mock<ISsrPageCacheInvalidator>(MockBehavior.Strict);
        ssrPageCacheInvalidator
            .Setup(invalidator => invalidator.InvalidateAsync(It.IsAny<SsrPageCacheInvalidationRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        Mock<ISsrPageCacheInvalidationRequestResolver> resolver = CreateResolver(CreateTargetedRequest());
        InvalidatePublicCachesFilter filter = CreateFilter(outputCacheStore, ssrPageCacheInvalidator, resolver);
        ActionExecutingContext context = CreateExecutingContext(
            HttpMethods.Post,
            new InvalidatesPublicCacheAttribute(PublicCacheScope.Data) { EvictOutputCache = false });

        await filter.OnActionExecutionAsync(context, () => Task.FromResult(CreateExecutedContext(context)));

        outputCacheStore.Verify(
            store => store.EvictByTagAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
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

    [Fact]
    public async Task OnActionExecutionAsync_WhenTargetedMutationIsSafe_ShouldKeepStaleRefreshEnabled()
    {
        SsrPageCacheInvalidationRequest capturedRequest = null!;
        Mock<IOutputCacheStore> outputCacheStore = CreateOutputCacheStore(ApiOutputCachePolicyNames.PublicDataTag);
        Mock<ISsrPageCacheInvalidator> ssrPageCacheInvalidator = new Mock<ISsrPageCacheInvalidator>(MockBehavior.Strict);
        ssrPageCacheInvalidator
            .Setup(invalidator => invalidator.InvalidateAsync(It.IsAny<SsrPageCacheInvalidationRequest>(), It.IsAny<CancellationToken>()))
            .Callback<SsrPageCacheInvalidationRequest, CancellationToken>((request, _) => capturedRequest = request)
            .Returns(Task.CompletedTask);
        Mock<ISsrPageCacheInvalidationRequestResolver> resolver = CreateResolver(CreateTargetedRequest());
        InvalidatePublicCachesFilter filter = CreateFilter(outputCacheStore, ssrPageCacheInvalidator, resolver);
        ActionExecutingContext context = CreateExecutingContext(
            HttpMethods.Put,
            new InvalidatesPublicCacheAttribute(PublicCacheScope.Data));

        await filter.OnActionExecutionAsync(context, () => Task.FromResult(CreateExecutedContext(context)));

        Assert.True(capturedRequest.AllowStale);
        Assert.True(capturedRequest.Refresh);
        outputCacheStore.VerifyAll();
        ssrPageCacheInvalidator.VerifyAll();
    }

    [Fact]
    public async Task OnActionExecutionAsync_WhenDeletingPublicContent_ShouldForceHardSsrPurge()
    {
        SsrPageCacheInvalidationRequest capturedRequest = null!;
        Mock<IOutputCacheStore> outputCacheStore = CreateOutputCacheStore(ApiOutputCachePolicyNames.PublicDataTag);
        Mock<ISsrPageCacheInvalidator> ssrPageCacheInvalidator = new Mock<ISsrPageCacheInvalidator>(MockBehavior.Strict);
        ssrPageCacheInvalidator
            .Setup(invalidator => invalidator.InvalidateAsync(It.IsAny<SsrPageCacheInvalidationRequest>(), It.IsAny<CancellationToken>()))
            .Callback<SsrPageCacheInvalidationRequest, CancellationToken>((request, _) => capturedRequest = request)
            .Returns(Task.CompletedTask);
        Mock<ISsrPageCacheInvalidationRequestResolver> resolver = CreateResolver(CreateTargetedRequest());
        InvalidatePublicCachesFilter filter = CreateFilter(outputCacheStore, ssrPageCacheInvalidator, resolver);
        ActionExecutingContext context = CreateExecutingContext(
            HttpMethods.Delete,
            new InvalidatesPublicCacheAttribute(PublicCacheScope.Data));

        await filter.OnActionExecutionAsync(context, () => Task.FromResult(CreateExecutedContext(context)));

        Assert.False(capturedRequest.AllowStale);
        Assert.False(capturedRequest.Refresh);
        outputCacheStore.VerifyAll();
        ssrPageCacheInvalidator.VerifyAll();
    }

    [Fact]
    public async Task OnActionExecutionAsync_WhenVisibilityIsDisabled_ShouldForceHardSsrPurge()
    {
        SsrPageCacheInvalidationRequest capturedRequest = null!;
        Mock<IOutputCacheStore> outputCacheStore = CreateOutputCacheStore(ApiOutputCachePolicyNames.PublicDataTag);
        Mock<ISsrPageCacheInvalidator> ssrPageCacheInvalidator = new Mock<ISsrPageCacheInvalidator>(MockBehavior.Strict);
        ssrPageCacheInvalidator
            .Setup(invalidator => invalidator.InvalidateAsync(It.IsAny<SsrPageCacheInvalidationRequest>(), It.IsAny<CancellationToken>()))
            .Callback<SsrPageCacheInvalidationRequest, CancellationToken>((request, _) => capturedRequest = request)
            .Returns(Task.CompletedTask);
        Mock<ISsrPageCacheInvalidationRequestResolver> resolver = CreateResolver(CreateTargetedRequest());
        InvalidatePublicCachesFilter filter = CreateFilter(outputCacheStore, ssrPageCacheInvalidator, resolver);
        ActionExecutingContext context = CreateExecutingContext(
            HttpMethods.Patch,
            new InvalidatesPublicCacheAttribute(PublicCacheScope.Data),
            new Dictionary<string, object?> { ["request"] = new VisibilityRequest { IsVisible = false } });

        await filter.OnActionExecutionAsync(context, () => Task.FromResult(CreateExecutedContext(context)));

        Assert.False(capturedRequest.AllowStale);
        Assert.False(capturedRequest.Refresh);
        outputCacheStore.VerifyAll();
        ssrPageCacheInvalidator.VerifyAll();
    }

    [Fact]
    public async Task OnActionExecutionAsync_WhenMarkedNotRelevant_ShouldForceHardSsrPurge()
    {
        SsrPageCacheInvalidationRequest capturedRequest = null!;
        Mock<IOutputCacheStore> outputCacheStore = CreateOutputCacheStore(ApiOutputCachePolicyNames.PublicDataTag);
        Mock<ISsrPageCacheInvalidator> ssrPageCacheInvalidator = new Mock<ISsrPageCacheInvalidator>(MockBehavior.Strict);
        ssrPageCacheInvalidator
            .Setup(invalidator => invalidator.InvalidateAsync(It.IsAny<SsrPageCacheInvalidationRequest>(), It.IsAny<CancellationToken>()))
            .Callback<SsrPageCacheInvalidationRequest, CancellationToken>((request, _) => capturedRequest = request)
            .Returns(Task.CompletedTask);
        Mock<ISsrPageCacheInvalidationRequestResolver> resolver = CreateResolver(CreateTargetedRequest());
        InvalidatePublicCachesFilter filter = CreateFilter(outputCacheStore, ssrPageCacheInvalidator, resolver);
        ActionExecutingContext context = CreateExecutingContext(
            HttpMethods.Patch,
            new InvalidatesPublicCacheAttribute(PublicCacheScope.Data),
            new Dictionary<string, object?> { ["request"] = new ReviewStatusRequest { AdminReviewStatus = "NotRelevant" } });

        await filter.OnActionExecutionAsync(context, () => Task.FromResult(CreateExecutedContext(context)));

        Assert.False(capturedRequest.AllowStale);
        Assert.False(capturedRequest.Refresh);
        outputCacheStore.VerifyAll();
        ssrPageCacheInvalidator.VerifyAll();
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

    private static Mock<ISsrPageCacheInvalidationRequestResolver> CreateResolver(SsrPageCacheInvalidationRequest? request = null)
    {
        Mock<ISsrPageCacheInvalidationRequestResolver> resolver = new Mock<ISsrPageCacheInvalidationRequestResolver>(MockBehavior.Strict);
        resolver
            .Setup(value => value.ResolveAsync(
                It.IsAny<ActionExecutingContext>(),
                It.IsAny<ActionExecutedContext?>(),
                It.IsAny<IReadOnlyCollection<PublicCacheScope>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(request ?? SsrPageCacheInvalidationRequest.AllCaches());
        return resolver;
    }

    private static Mock<IOutputCacheStore> CreateOutputCacheStore(string tag)
    {
        Mock<IOutputCacheStore> outputCacheStore = new Mock<IOutputCacheStore>(MockBehavior.Strict);
        outputCacheStore
            .Setup(store => store.EvictByTagAsync(tag, It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        return outputCacheStore;
    }

    private static SsrPageCacheInvalidationRequest CreateTargetedRequest()
    {
        return new SsrPageCacheInvalidationRequest
        {
            Paths = new[] { "/fr/home" },
            IncludeSeoDocuments = true,
        };
    }

    private static SsrPageCacheInvalidationRequest CreateNoOpRequest()
    {
        return new SsrPageCacheInvalidationRequest();
    }

    private static ActionExecutingContext CreateExecutingContext(
        string method,
        InvalidatesPublicCacheAttribute attribute,
        Dictionary<string, object?>? actionArguments = null)
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
            actionArguments ?? new Dictionary<string, object?>(),
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

    private sealed class VisibilityRequest
    {
        public bool IsVisible { get; init; }
    }

    private sealed class ReviewStatusRequest
    {
        public string AdminReviewStatus { get; init; } = string.Empty;
    }
}
