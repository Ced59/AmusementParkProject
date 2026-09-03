using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Passport.Models;
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
        Mock<IParkItemRepository> repository =
            new Mock<IParkItemRepository>(MockBehavior.Strict);
        repository.Setup(value => value.GetByIdsAsync(
                It.Is<IReadOnlyCollection<string>>(ids => ids.Single() == "item-1"),
                CancellationToken.None))
            .ReturnsAsync(new[]
            {
                new ParkItem
                {
                    Id = "item-1",
                    ParkId = "park-1",
                    Name = "Ancienne attraction",
                    Category = ParkItemCategory.Attraction,
                    IsVisible = false,
                    AttractionDetails = new AttractionDetails
                    {
                        OpeningDate = new DateTime(1998, 4, 1),
                        ClosingDate = new DateTime(2010, 9, 30),
                    },
                },
            });
        VisitTargetResolver resolver = new VisitTargetResolver(repository.Object);

        IReadOnlyDictionary<string, VisitTarget> result = await resolver.ResolveAsync(
            new[] { "item-1" },
            CancellationToken.None);

        VisitTarget target = Assert.Single(result).Value;
        Assert.Equal("park-1", target.ParkId);
        Assert.Equal(new DateOnly(1998, 4, 1), target.OpeningDate);
        Assert.Equal(new DateOnly(2010, 9, 30), target.ClosingDate);
        repository.VerifyAll();
    }
}
