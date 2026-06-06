using AmusementPark.Core.Domain.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Mappers;

public sealed class AdminReviewStatusMongoMappersTests
{
    [Theory]
    [InlineData(AdminReviewStatus.ToReview, AdminReviewStatus.ToReview)]
    [InlineData(AdminReviewStatus.Validated, AdminReviewStatus.Validated)]
    [InlineData(AdminReviewStatus.Ready, AdminReviewStatus.Validated)]
    [InlineData(AdminReviewStatus.ToProcessLater, AdminReviewStatus.ToProcessLater)]
    [InlineData(AdminReviewStatus.NotRelevant, AdminReviewStatus.NotRelevant)]
    public void NormalizeForAdministration_WhenStatusProvided_ShouldReturnExpectedStatus(AdminReviewStatus value, AdminReviewStatus expected)
    {
        AdminReviewStatus result = value.NormalizeForAdministration();

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(AdminReviewStatus.ToReview, 0)]
    [InlineData(AdminReviewStatus.Validated, 10)]
    [InlineData(AdminReviewStatus.Ready, 10)]
    [InlineData(AdminReviewStatus.ToProcessLater, 90)]
    [InlineData(AdminReviewStatus.NotRelevant, 99)]
    public void ToAdminReviewPriority_WhenStatusProvided_ShouldReturnExpectedPriority(AdminReviewStatus value, int expected)
    {
        int result = value.ToAdminReviewPriority();

        Assert.Equal(expected, result);
    }
}
