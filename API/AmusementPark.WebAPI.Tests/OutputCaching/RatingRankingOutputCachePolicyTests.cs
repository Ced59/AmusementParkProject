using System.Reflection;
using AmusementPark.WebAPI.Controllers;
using AmusementPark.WebAPI.OutputCaching;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OutputCaching;
using Xunit;

namespace AmusementPark.WebAPI.Tests.OutputCaching;

public sealed class RatingRankingOutputCachePolicyTests
{
    [Fact]
    public async Task GenerationPolicy_WhenAdvanced_ShouldChangeTheOutputCacheKeyDimension()
    {
        RatingRankingGenerationOutputCachePolicy policy = new RatingRankingGenerationOutputCachePolicy();
        OutputCacheContext initialContext = new OutputCacheContext
        {
            HttpContext = new DefaultHttpContext(),
        };
        OutputCacheContext advancedContext = new OutputCacheContext
        {
            HttpContext = new DefaultHttpContext(),
        };

        await policy.CacheRequestAsync(initialContext, CancellationToken.None);
        policy.Advance();
        await policy.CacheRequestAsync(advancedContext, CancellationToken.None);

        Assert.Equal("0", initialContext.CacheVaryByRules.VaryByValues[RatingRankingGenerationOutputCachePolicy.VaryByKey]);
        Assert.Equal("1", advancedContext.CacheVaryByRules.VaryByValues[RatingRankingGenerationOutputCachePolicy.VaryByKey]);
    }

    [Theory]
    [InlineData(typeof(RatingsController), nameof(RatingsController.GetSummaryAsync), ApiOutputCachePolicyNames.PublicRatingDataShort)]
    [InlineData(typeof(RatingsController), nameof(RatingsController.GetRankingsAsync), ApiOutputCachePolicyNames.PublicRatingDataShort)]
    [InlineData(typeof(RatingsController), nameof(RatingsController.GetParkItemRankingsAsync), ApiOutputCachePolicyNames.PublicRatingDataShort)]
    [InlineData(typeof(ParksController), nameof(ParksController.GetParkDetailSummaryAsync), ApiOutputCachePolicyNames.PublicParkDetailData)]
    [InlineData(typeof(ParkItemsController), nameof(ParkItemsController.GetByIdAsync), ApiOutputCachePolicyNames.PublicParkItemDetailData)]
    public void PublicRatingResponse_ShouldUseRatingTaggedOutputCachePolicy(
        Type controllerType,
        string methodName,
        string expectedPolicyName)
    {
        MethodInfo? method = controllerType.GetMethod(methodName);

        Assert.NotNull(method);
        OutputCacheAttribute? attribute = method.GetCustomAttribute<OutputCacheAttribute>();
        Assert.NotNull(attribute);
        Assert.Equal(expectedPolicyName, attribute.PolicyName);
    }
}
