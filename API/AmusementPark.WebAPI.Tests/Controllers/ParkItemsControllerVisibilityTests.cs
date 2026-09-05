using AmusementPark.WebAPI.Controllers;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Controllers;

public sealed class ParkItemsControllerVisibilityTests
{
    [Theory]
    [InlineData(false, null, false)]
    [InlineData(false, true, false)]
    [InlineData(true, null, true)]
    [InlineData(true, true, true)]
    [InlineData(true, false, false)]
    public void ResolveIncludeHidden_ShouldRespectPublicSafetyAndExplicitAdminFiltering(
        bool userCanSeeNonVisible,
        bool? requestedIncludeHidden,
        bool expected)
    {
        Assert.Equal(
            expected,
            ParkItemsController.ResolveIncludeHidden(userCanSeeNonVisible, requestedIncludeHidden));
    }
}
