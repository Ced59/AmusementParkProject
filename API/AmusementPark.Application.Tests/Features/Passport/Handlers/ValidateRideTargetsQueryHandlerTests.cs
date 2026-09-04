using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Passport.Handlers;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Application.Features.Passport.Queries;
using AmusementPark.Core.Domain.Parks;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Passport.Handlers;

public sealed class ValidateRideTargetsQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldAcceptExistingAttractionsFromTheRequestedPark()
    {
        Mock<IVisitTargetResolver> resolver =
            new Mock<IVisitTargetResolver>(MockBehavior.Strict);
        resolver.Setup(value => value.ResolveAsync(
                It.Is<IReadOnlyCollection<string>>(ids =>
                    ids.SequenceEqual(new[] { "item-1", "item-2" })),
                CancellationToken.None))
            .ReturnsAsync(new Dictionary<string, VisitTarget>(StringComparer.Ordinal)
            {
                ["item-1"] = CreateTarget("item-1", "park-1"),
                ["item-2"] = CreateTarget("item-2", "park-1"),
            });
        ValidateRideTargetsQueryHandler handler =
            new ValidateRideTargetsQueryHandler(resolver.Object);

        ApplicationResult<bool> result = await handler.HandleAsync(
            new ValidateRideTargetsQuery(
                "owner-1",
                "park-1",
                new[] { "item-1", "item-2", "item-1" }),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value);
        resolver.VerifyAll();
    }

    [Theory]
    [InlineData("missing", "park-1", ParkItemCategory.Attraction, "ride-occurrence.target-not-found")]
    [InlineData("item-1", "park-2", ParkItemCategory.Attraction, "ride-occurrence.target-park-mismatch")]
    [InlineData("item-1", "park-1", ParkItemCategory.Restaurant, "ride-occurrence.target-not-attraction")]
    public async Task HandleAsync_ShouldUseTheSameIdentityRulesAsRideCreation(
        string resolvedId,
        string resolvedParkId,
        ParkItemCategory category,
        string expectedErrorCode)
    {
        Mock<IVisitTargetResolver> resolver =
            new Mock<IVisitTargetResolver>(MockBehavior.Strict);
        IReadOnlyDictionary<string, VisitTarget> targets = resolvedId == "missing"
            ? new Dictionary<string, VisitTarget>()
            : new Dictionary<string, VisitTarget>
            {
                [resolvedId] = CreateTarget(resolvedId, resolvedParkId, category),
            };
        resolver.Setup(value => value.ResolveAsync(
                It.Is<IReadOnlyCollection<string>>(ids => ids.Single() == "item-1"),
                CancellationToken.None))
            .ReturnsAsync(targets);
        ValidateRideTargetsQueryHandler handler =
            new ValidateRideTargetsQueryHandler(resolver.Object);

        ApplicationResult<bool> result = await handler.HandleAsync(
            new ValidateRideTargetsQuery("owner-1", "park-1", new[] { "item-1" }),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(expectedErrorCode, Assert.Single(result.Errors).Code);
        resolver.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_ShouldRejectAnInvalidScopeBeforeResolvingTargets()
    {
        Mock<IVisitTargetResolver> resolver =
            new Mock<IVisitTargetResolver>(MockBehavior.Strict);
        ValidateRideTargetsQueryHandler handler =
            new ValidateRideTargetsQueryHandler(resolver.Object);

        ApplicationResult<bool> result = await handler.HandleAsync(
            new ValidateRideTargetsQuery("owner-1", " ", Array.Empty<string?>()),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("ride-occurrence.batch-invalid", Assert.Single(result.Errors).Code);
        resolver.VerifyNoOtherCalls();
    }

    private static VisitTarget CreateTarget(
        string id,
        string parkId,
        ParkItemCategory category = ParkItemCategory.Attraction)
    {
        return new VisitTarget(id, parkId, "Cible", category, null, null, null);
    }
}
