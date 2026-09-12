using AmusementPark.Core.Domain.Visits;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Visits;

public sealed class PassportProfileSourceMutationPolicyTests
{
    [Theory]
    [InlineData(VisitStatus.Draft, VisitStatus.Draft, false)]
    [InlineData(VisitStatus.Draft, VisitStatus.Completed, true)]
    [InlineData(VisitStatus.Completed, VisitStatus.Completed, true)]
    [InlineData(VisitStatus.Completed, VisitStatus.Draft, true)]
    [InlineData(VisitStatus.Archived, VisitStatus.Draft, false)]
    public void CanChangeCompletedVisitProjection_ShouldFollowCompletedMembership(
        VisitStatus previousStatus,
        VisitStatus nextStatus,
        bool expected)
    {
        bool result = PassportProfileSourceMutationPolicy
            .CanChangeCompletedVisitProjection(previousStatus, nextStatus);

        Assert.Equal(expected, result);
    }
}
