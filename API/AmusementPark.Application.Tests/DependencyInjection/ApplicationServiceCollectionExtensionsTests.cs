using AmusementPark.Application.DependencyInjection;
using AmusementPark.Application.Features.History.Services;
using AmusementPark.Application.Features.LiveData.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AmusementPark.Application.Tests.DependencyInjection;

public sealed class ApplicationServiceCollectionExtensionsTests
{
    [Fact]
    public void AddApplication_RegistersHistoricalRolloutAssessmentAsScoped()
    {
        ServiceCollection services = new ServiceCollection();

        services.AddApplication();

        ServiceDescriptor registration = Assert.Single(
            services,
            static descriptor =>
                descriptor.ServiceType == typeof(IHistoricalParkRolloutGateAssessmentService));
        Assert.Equal(ServiceLifetime.Scoped, registration.Lifetime);
    }

    [Fact]
    public void AddApplication_RegistersLiveOperationalWriteCoordinatorAsSingleton()
    {
        ServiceCollection services = new ServiceCollection();

        services.AddApplication();

        ServiceDescriptor registration = Assert.Single(
            services,
            static descriptor =>
                descriptor.ServiceType == typeof(LiveOperationalWriteCoordinator));
        Assert.Equal(ServiceLifetime.Singleton, registration.Lifetime);
    }
}
