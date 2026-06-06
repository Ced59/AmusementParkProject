using AmusementPark.Core.Domain.Parks;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Parks;

public sealed class AdminReviewStatusTests
{
    [Fact]
    public void Ready_WhenComparedToValidated_ShouldRemainBackwardCompatibleAlias()
    {
        Assert.Equal(AdminReviewStatus.Validated, AdminReviewStatus.Ready);
        Assert.Equal((int)AdminReviewStatus.Validated, (int)AdminReviewStatus.Ready);
    }

    [Fact]
    public void DefaultValue_WhenCreatedByClr_ShouldBeToReview()
    {
        AdminReviewStatus status = default;

        Assert.Equal(AdminReviewStatus.ToReview, status);
    }
}
