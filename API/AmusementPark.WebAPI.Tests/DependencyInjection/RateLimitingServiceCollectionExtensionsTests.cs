using System.Collections.Generic;
using System.Net;
using System.Security.Claims;
using System.Threading.RateLimiting;
using AmusementPark.WebAPI.DependencyInjection;
using AmusementPark.WebAPI.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AmusementPark.WebAPI.Tests.DependencyInjection;

public sealed class RateLimitingServiceCollectionExtensionsTests
{
    [Fact]
    public void CreatePreAuthenticationIpLimiter_ShouldKeepPublicReadsSeparateFromTheGeneralQuota()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["IpRateLimiting:GeneralRules:0:Limit"] = "1",
                ["IpRateLimiting:GeneralRules:0:Period"] = "60s",
                ["RateLimiting:PublicReads:PermitLimit"] = "2",
                ["RateLimiting:PublicReads:WindowSeconds"] = "60",
            })
            .Build();
        using PartitionedRateLimiter<HttpContext> limiter =
            RateLimitingServiceCollectionExtensions.CreatePreAuthenticationIpLimiter(
                configuration);

        using RateLimitLease firstRead = limiter.AttemptAcquire(CreateContext(HttpMethods.Get));
        using RateLimitLease secondRead = limiter.AttemptAcquire(CreateContext(HttpMethods.Head));
        using RateLimitLease rejectedRead = limiter.AttemptAcquire(CreateContext(HttpMethods.Get));
        using RateLimitLease firstWrite = limiter.AttemptAcquire(CreateContext(HttpMethods.Post));
        using RateLimitLease rejectedWrite = limiter.AttemptAcquire(CreateContext(HttpMethods.Post));

        Assert.True(firstRead.IsAcquired);
        Assert.True(secondRead.IsAcquired);
        Assert.False(rejectedRead.IsAcquired);
        Assert.True(firstWrite.IsAcquired);
        Assert.False(rejectedWrite.IsAcquired);
    }

    [Fact]
    public void AddApiRateLimiting_ShouldRegisterPreAuthenticationIpMiddleware()
    {
        IConfiguration configuration = new ConfigurationBuilder().Build();
        ServiceCollection services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddApiRateLimiting(configuration);

        using ServiceProvider serviceProvider = services.BuildServiceProvider();

        Assert.NotNull(
            serviceProvider.GetRequiredService<PreAuthenticationIpRateLimitingMiddleware>());
    }

    [Fact]
    public void GetSharePublicationPreviewPartitionKey_ShouldPreferAuthenticatedUser()
    {
        DefaultHttpContext context = CreateContext(HttpMethods.Post);
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, "owner-1") },
            "Test"));

        string result = RateLimitingServiceCollectionExtensions
            .GetSharePublicationPreviewPartitionKey(context);

        Assert.Equal("share-publication-preview:user:owner-1", result);
    }

    [Fact]
    public void GetSharePublicationPreviewPartitionKey_ShouldFallBackToRemoteIp()
    {
        DefaultHttpContext context = CreateContext(HttpMethods.Post);

        string result = RateLimitingServiceCollectionExtensions
            .GetSharePublicationPreviewPartitionKey(context);

        Assert.Equal("share-publication-preview:ip:203.0.113.10", result);
    }

    [Fact]
    public void GetSharePublicationConfirmationPartitionKey_ShouldUseASeparateUserBudget()
    {
        DefaultHttpContext context = CreateContext(HttpMethods.Post);
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, "owner-1") },
            "Test"));

        string result = RateLimitingServiceCollectionExtensions
            .GetSharePublicationConfirmationPartitionKey(context);

        Assert.Equal("share-publication-confirmation:user:owner-1", result);
        Assert.NotEqual(
            RateLimitingServiceCollectionExtensions
                .GetSharePublicationPreviewPartitionKey(context),
            result);
    }

    [Fact]
    public void GetNotificationEmailUnsubscribePartitionKey_ShouldHashTheTokenInsteadOfUsingTheIp()
    {
        DefaultHttpContext firstContext = CreateContext(HttpMethods.Post);
        firstContext.Request.QueryString = new QueryString("?token=opaque-token-1");
        DefaultHttpContext secondContext = CreateContext(HttpMethods.Post);
        secondContext.Connection.RemoteIpAddress = IPAddress.Parse("198.51.100.20");
        secondContext.Request.QueryString = new QueryString("?token=opaque-token-1");

        string firstKey = RateLimitingServiceCollectionExtensions
            .GetNotificationEmailUnsubscribePartitionKey(firstContext);
        string secondKey = RateLimitingServiceCollectionExtensions
            .GetNotificationEmailUnsubscribePartitionKey(secondContext);

        Assert.Equal(firstKey, secondKey);
        Assert.StartsWith("notification-email-unsubscribe:token:", firstKey);
        Assert.DoesNotContain("opaque-token-1", firstKey, StringComparison.Ordinal);
    }

    [Fact]
    public void GetNotificationEmailUnsubscribePartitionKey_ShouldFallBackToTheIpWithoutAToken()
    {
        DefaultHttpContext context = CreateContext(HttpMethods.Post);

        string result = RateLimitingServiceCollectionExtensions
            .GetNotificationEmailUnsubscribePartitionKey(context);

        Assert.Equal("notification-email-unsubscribe:ip:203.0.113.10", result);
    }

    private static DefaultHttpContext CreateContext(string method)
    {
        DefaultHttpContext context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.10");
        context.Request.Method = method;
        return context;
    }
}
