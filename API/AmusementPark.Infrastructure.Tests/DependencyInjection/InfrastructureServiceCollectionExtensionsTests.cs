using AmusementPark.Application.Features.BackgroundJobs.Ports;
using AmusementPark.Application.Features.TechnicalStats.Ports;
using AmusementPark.Infrastructure.Configuration.BackgroundJobs;
using AmusementPark.Infrastructure.DependencyInjection;
using AmusementPark.Infrastructure.Services.BackgroundJobs;
using AmusementPark.Infrastructure.Services.Ratings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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

    [Fact]
    public void AddInfrastructure_WhenCalled_ShouldRegisterDurableBackgroundJobRepository()
    {
        ServiceCollection services = new ServiceCollection();
        IConfiguration configuration = new ConfigurationBuilder().Build();

        services.AddInfrastructure(configuration);

        ServiceDescriptor registration = Assert.Single(
            services,
            static service => service.ServiceType == typeof(IDurableBackgroundJobRepository));
        Assert.Equal(ServiceLifetime.Scoped, registration.Lifetime);
    }

    [Fact]
    public void AddInfrastructure_WhenCalled_ShouldRegisterTheBoundedDurableWorker()
    {
        ServiceCollection services = new ServiceCollection();
        IConfiguration configuration = new ConfigurationBuilder().Build();

        services.AddInfrastructure(configuration);

        Assert.Contains(
            services,
            static service =>
                service.ServiceType == typeof(IHostedService) &&
                service.ImplementationType == typeof(DurableBackgroundJobWorkerBackgroundService));
        ServiceDescriptor settingsRegistration = Assert.Single(
            services,
            static service => service.ServiceType == typeof(DurableBackgroundJobWorkerSettings));
        Assert.Equal(ServiceLifetime.Singleton, settingsRegistration.Lifetime);
    }

    [Fact]
    public void AddInfrastructure_WhenCalled_ShouldRegisterRankingRebuildReconciliation()
    {
        ServiceCollection services = new ServiceCollection();
        IConfiguration configuration = new ConfigurationBuilder().Build();

        services.AddInfrastructure(configuration);

        Assert.Contains(
            services,
            static service =>
                service.ServiceType == typeof(IHostedService) &&
                service.ImplementationType == typeof(RatingRankingRebuildReconciliationBackgroundService));
    }

}
