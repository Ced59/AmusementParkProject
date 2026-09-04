using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Application.Features.Passport.Services;
using AmusementPark.Core.Domain.Parks;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Passport.Services;

public sealed class VisitTargetResolverTests
{
    [Fact]
    public async Task ResolveAsync_ShouldMapHistoricalBoundsWithoutFilteringHiddenTargets()
    {
        Mock<IVisitTargetReadRepository> repository =
            new Mock<IVisitTargetReadRepository>(MockBehavior.Strict);
        repository.Setup(value => value.GetByIdsAsync(
                It.Is<IReadOnlyCollection<string>>(ids => ids.Single() == "item-1"),
                CancellationToken.None))
            .ReturnsAsync(new[]
            {
                new VisitTarget(
                    "item-1",
                    "park-1",
                    "Ancienne attraction",
                    ParkItemCategory.Attraction,
                    new DateOnly(1998, 4, 1),
                    new DateOnly(2010, 9, 30),
                    "ClosedDefinitively",
                    false),
            });
        VisitTargetResolver resolver = new VisitTargetResolver(repository.Object);

        IReadOnlyDictionary<string, VisitTarget> result = await resolver.ResolveAsync(
            new[] { "item-1" },
            CancellationToken.None);

        VisitTarget target = Assert.Single(result).Value;
        Assert.Equal("park-1", target.ParkId);
        Assert.Equal(new DateOnly(1998, 4, 1), target.OpeningDate);
        Assert.Equal(new DateOnly(2010, 9, 30), target.ClosingDate);
        Assert.Equal("ClosedDefinitively", target.LifecycleStatus);
        Assert.False(target.IsVisible);
        repository.VerifyAll();
    }
}
