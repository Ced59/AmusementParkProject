using AmusementPark.Application.Features.TechnicalStats.Ports;
using AmusementPark.Infrastructure.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.DependencyInjection;

public sealed class InfrastructureServiceCollectionExtensionsTests
{
    [Fact]
    public void AddInfrastructure_WhenCalled_ShouldRegisterTechnicalStatsProvider()
    {
        ServiceCollection services = new ServiceCollection();
        IConfiguration configuration = new ConfigurationBuilder().Build();

        services.AddInfrastructure(configuration);

        Assert.Contains(services, static service => service.ServiceType == typeof(ITechnicalStatsProvider));
    }
}
