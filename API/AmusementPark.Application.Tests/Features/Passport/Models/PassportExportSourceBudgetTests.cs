using AmusementPark.Application.Features.Passport.Models;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Passport.Models;

public sealed class PassportExportSourceBudgetTests
{
    [Fact]
    public void TryConsume_RejectsCumulativeOverflowWithoutChangingConsumption()
    {
        PassportExportSourceBudget budget = new PassportExportSourceBudget(10);

        Assert.True(budget.TryConsume(7));
        Assert.False(budget.TryConsume(4));
        Assert.Equal(7, budget.ConsumedBytes);
        Assert.True(budget.TryConsume(3));
        Assert.Equal(10, budget.ConsumedBytes);
    }
}
