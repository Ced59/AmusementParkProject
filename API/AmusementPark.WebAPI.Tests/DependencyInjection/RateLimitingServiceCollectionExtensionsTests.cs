using System.Collections.Generic;
using System.Net;
using System.Threading.RateLimiting;
using AmusementPark.WebAPI.DependencyInjection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace AmusementPark.WebAPI.Tests.DependencyInjection;

public sealed class RateLimitingServiceCollectionExtensionsTests
{
    [Fact]
    public void CreateParkDataEditorOperationStatusLimiterOptions_ShouldRejectImmediateSecondPoll()
    {
        TokenBucketRateLimiterOptions options =
            RateLimitingServiceCollectionExtensions.CreateParkDataEditorOperationStatusLimiterOptions();
        using TokenBucketRateLimiter limiter = new TokenBucketRateLimiter(options);

        using RateLimitLease firstPoll = limiter.AttemptAcquire();
        using RateLimitLease immediateSecondPoll = limiter.AttemptAcquire();

        Assert.True(firstPoll.IsAcquired);
        Assert.False(immediateSecondPoll.IsAcquired);
        Assert.Equal(TimeSpan.FromSeconds(5), options.ReplenishmentPeriod);
    }

    [Fact]
    public void AddApiRateLimiting_ShouldKeepPublicReadsSeparateFromTheGeneralQuota()
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
        ServiceCollection services = new ServiceCollection();
        services.AddApiRateLimiting(configuration);

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        RateLimiterOptions options = serviceProvider
            .GetRequiredService<IOptions<RateLimiterOptions>>()
            .Value;
        PartitionedRateLimiter<HttpContext> limiter = Assert.IsAssignableFrom<PartitionedRateLimiter<HttpContext>>(
            options.GlobalLimiter);

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

    private static DefaultHttpContext CreateContext(string method)
    {
        DefaultHttpContext context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.10");
        context.Request.Method = method;
        return context;
    }
}
