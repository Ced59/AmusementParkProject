using AmusementPark.WebAPI.Configuration;
using AmusementPark.WebAPI.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Xunit;

namespace AmusementPark.WebAPI.Tests.DependencyInjection;

public sealed class CorsServiceCollectionExtensionsTests
{
    [Fact]
    public void AddApiCors_ShouldKeepRequiredPassportHeadersWithExplicitConfiguration()
    {
        Dictionary<string, string?> values = new Dictionary<string, string?>
        {
            ["Cors:AllowedOrigins:0"] = "http://localhost:4200",
            ["Cors:AllowedHeaders:0"] = "Authorization",
            ["Cors:ExposedHeaders:0"] = "Retry-After",
        };
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
        Mock<IHostEnvironment> environment = new Mock<IHostEnvironment>();
        environment.SetupGet(value => value.EnvironmentName)
            .Returns(Environments.Development);
        ServiceCollection services = new ServiceCollection();

        services.AddApiCors(configuration, environment.Object);

        using ServiceProvider provider = services.BuildServiceProvider();
        CorsSettings settings = provider.GetRequiredService<CorsSettings>();
        Assert.Contains(
            settings.AllowedHeaders,
            static header => string.Equals(
                header,
                "Idempotency-Key",
                StringComparison.OrdinalIgnoreCase));
        Assert.Contains(
            settings.ExposedHeaders,
            static header => string.Equals(
                header,
                "Idempotency-Replayed",
                StringComparison.OrdinalIgnoreCase));
        Assert.Contains(
            settings.ExposedHeaders,
            static header => string.Equals(
                header,
                "Ride-Order-Normalized",
                StringComparison.OrdinalIgnoreCase));
    }
}
