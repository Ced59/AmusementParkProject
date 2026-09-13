using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Queries;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Core.Domain.Sharing;
using AmusementPark.WebAPI.Contracts.Sharing;
using AmusementPark.WebAPI.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Controllers;

public sealed class SharedProfileComparisonsControllerTests
{
    [Fact]
    public async Task GetAsync_ShouldReturnOnlyPublicComparisonFacts()
    {
        ProfileComparisonCalculation calculation = new ProfileComparisonCalculation(
            "Camille",
            "Alex",
            new[] { ProfileComparisonCategory.VisitedParks },
            new[] { new ProfileComparisonParkResult("Phantasialand", "DE", 2, 4) },
            Array.Empty<ProfileComparisonRatingResult>(),
            Array.Empty<ProfileComparisonYearResult>(),
            Array.Empty<ProfileComparisonMissedItemResult>(),
            0,
            ProfileComparisonCalculator.MinimumRatingsForCorrelation,
            null,
            false,
            ProfileComparisonCalculator.CalculationVersion);
        Mock<IQueryHandler<GetSharedProfileComparisonQuery,
            ApplicationResult<SharedProfileComparisonResult>>> handler = new(MockBehavior.Strict);
        handler.Setup(value => value.HandleAsync(
                new GetSharedProfileComparisonQuery("opaque-share"),
                CancellationToken.None))
            .ReturnsAsync(ApplicationResult<SharedProfileComparisonResult>.Success(
                new SharedProfileComparisonResult(
                    new DateTime(2026, 9, 13, 12, 0, 0, DateTimeKind.Utc),
                    calculation)));
        SharedProfileComparisonsController controller =
            new SharedProfileComparisonsController(handler.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext(),
                },
            };

        IActionResult result = await controller.GetAsync(
            "opaque-share",
            CancellationToken.None);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        SharedProfileComparisonDto response =
            Assert.IsType<SharedProfileComparisonDto>(ok.Value);
        Assert.Equal("Phantasialand", Assert.Single(response.Parks).Name);
        Assert.DoesNotContain(
            response.GetType().GetProperties(),
            static property => property.Name.EndsWith("Id", StringComparison.Ordinal));
        Assert.Equal("no-referrer", controller.Response.Headers["Referrer-Policy"]);
        handler.VerifyAll();
    }
}
