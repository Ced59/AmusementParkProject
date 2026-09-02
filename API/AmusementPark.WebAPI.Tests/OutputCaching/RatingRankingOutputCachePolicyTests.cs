using System.Reflection;
using AmusementPark.WebAPI.Controllers;
using AmusementPark.WebAPI.OutputCaching;
using Microsoft.AspNetCore.OutputCaching;
using Xunit;

namespace AmusementPark.WebAPI.Tests.OutputCaching;

public sealed class RatingRankingOutputCachePolicyTests
{
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
