using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Countries.Handlers;
using AmusementPark.Application.Features.Countries.Ports;
using AmusementPark.Application.Features.Countries.Queries;
using AmusementPark.Core.Domain.Countries;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Countries.Handlers;

public sealed class GetCountriesQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenRepositoryReturnsCountries_ShouldReturnSuccessWithSameCollection()
    {
        CancellationToken cancellationToken = new CancellationTokenSource().Token;
        Country[] countries = new[] { new Country { IsoCode = "FR" } };
        Mock<ICountryReadRepository> repository = new Mock<ICountryReadRepository>(MockBehavior.Strict);
        repository.Setup(item => item.GetAllAsync("fr", cancellationToken)).ReturnsAsync(countries);
        GetCountriesQueryHandler handler = new GetCountriesQueryHandler(repository.Object);

        ApplicationResult<IReadOnlyCollection<Country>> result = await handler.HandleAsync(new GetCountriesQuery("fr"), cancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Same(countries, result.Value);
        repository.VerifyAll();
    }
}
