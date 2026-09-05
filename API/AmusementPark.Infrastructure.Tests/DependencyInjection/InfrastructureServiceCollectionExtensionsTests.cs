using AmusementPark.Application.Features.BackgroundJobs.Ports;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Application.Features.TechnicalStats.Ports;
using AmusementPark.Infrastructure.Configuration.BackgroundJobs;
using AmusementPark.Infrastructure.DependencyInjection;
using AmusementPark.Infrastructure.Services.BackgroundJobs;
using AmusementPark.Infrastructure.Services.Passport;
using AmusementPark.Infrastructure.Services.Ratings;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
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
    public void AddInfrastructure_WhenCalled_ShouldRegisterUserVisitRepository()
    {
        ServiceCollection services = new ServiceCollection();
        IConfiguration configuration = new ConfigurationBuilder().Build();

        services.AddInfrastructure(configuration);

        ServiceDescriptor registration = Assert.Single(
            services,
            static service => service.ServiceType == typeof(IUserVisitRepository));
        Assert.Equal(typeof(UserVisitRepository), registration.ImplementationType);
        Assert.Equal(ServiceLifetime.Scoped, registration.Lifetime);
    }

    [Fact]
    public void AddInfrastructure_WhenCalled_ShouldRegisterRideOccurrenceRepository()
    {
        ServiceCollection services = new ServiceCollection();
        IConfiguration configuration = new ConfigurationBuilder().Build();

        services.AddInfrastructure(configuration);

        ServiceDescriptor registration = Assert.Single(
            services,
            static service => service.ServiceType == typeof(IRideOccurrenceRepository));
        Assert.Equal(typeof(UserRideOccurrenceRepository), registration.ImplementationType);
        Assert.Equal(ServiceLifetime.Scoped, registration.Lifetime);
    }

    [Fact]
    public void AddInfrastructure_WhenCalled_ShouldRegisterPassportItemStatisticsSourceReader()
    {
        ServiceCollection services = new ServiceCollection();
        IConfiguration configuration = new ConfigurationBuilder().Build();

        services.AddInfrastructure(configuration);

        ServiceDescriptor registration = Assert.Single(
            services,
            static service => service.ServiceType
                == typeof(IPassportItemStatisticsSourceReader));
        Assert.Equal(
            typeof(PassportItemStatisticsSourceReader),
            registration.ImplementationType);
        Assert.Equal(ServiceLifetime.Scoped, registration.Lifetime);
    }

    [Fact]
    public void AddInfrastructure_WhenCalled_ShouldRegisterPassportScopeStatisticsSourceReader()
    {
        ServiceCollection services = new ServiceCollection();
        IConfiguration configuration = new ConfigurationBuilder().Build();

        services.AddInfrastructure(configuration);

        ServiceDescriptor registration = Assert.Single(
            services,
            static service => service.ServiceType
                == typeof(IPassportScopeStatisticsSourceReader));
        Assert.Equal(
            typeof(PassportScopeStatisticsSourceReader),
            registration.ImplementationType);
        Assert.Equal(ServiceLifetime.Scoped, registration.Lifetime);
    }

    [Fact]
    public void AddInfrastructure_WhenCalled_ShouldRegisterGlobalRatingSuggestionPorts()
    {
        ServiceCollection services = new ServiceCollection();
        IConfiguration configuration = new ConfigurationBuilder().Build();

        services.AddInfrastructure(configuration);

        ServiceDescriptor source = Assert.Single(
            services,
            static service => service.ServiceType
                == typeof(IGlobalRatingSuggestionSourceReader));
        Assert.Equal(typeof(GlobalRatingSuggestionSourceReader), source.ImplementationType);
        Assert.Equal(ServiceLifetime.Scoped, source.Lifetime);
        ServiceDescriptor state = Assert.Single(
            services,
            static service => service.ServiceType
                == typeof(IGlobalRatingSuggestionStateRepository));
        Assert.Equal(typeof(GlobalRatingSuggestionStateRepository), state.ImplementationType);
        ServiceDescriptor outbox = Assert.Single(
            services,
            static service => service.ServiceType
                == typeof(IGlobalRatingSuggestionAnalyticsOutboxReconciler));
        Assert.Equal(typeof(GlobalRatingSuggestionStateRepository), outbox.ImplementationType);
        Assert.Equal(ServiceLifetime.Scoped, outbox.Lifetime);
        Assert.Contains(
            services,
            static service => service.ServiceType == typeof(IHostedService)
                && service.ImplementationType ==
                    typeof(GlobalRatingSuggestionAnalyticsOutboxBackgroundService));
    }

    [Fact]
    public void AddInfrastructure_WhenCalled_ShouldRegisterPassportClockAndTimeZoneValidator()
    {
        ServiceCollection services = new ServiceCollection();
        IConfiguration configuration = new ConfigurationBuilder().Build();

        services.AddInfrastructure(configuration);

        ServiceDescriptor clock = Assert.Single(
            services,
            static service => service.ServiceType == typeof(IPassportClock));
        Assert.Equal(typeof(SystemPassportClock), clock.ImplementationType);
        Assert.Equal(ServiceLifetime.Singleton, clock.Lifetime);
        ServiceDescriptor timeZones = Assert.Single(
            services,
            static service => service.ServiceType == typeof(IPassportTimeZoneValidator));
        Assert.Equal(typeof(SystemPassportTimeZoneValidator), timeZones.ImplementationType);
        ServiceDescriptor localDates = Assert.Single(
            services,
            static service => service.ServiceType == typeof(IPassportLocalDateResolver));
        Assert.Equal(typeof(SystemPassportLocalDateResolver), localDates.ImplementationType);
        Assert.Equal(ServiceLifetime.Singleton, timeZones.Lifetime);
    }

    [Fact]
    public void AddInfrastructure_WhenCalled_ShouldRegisterPrivatePassportAuditServices()
    {
        ServiceCollection services = new ServiceCollection();
        IConfiguration configuration = new ConfigurationBuilder().Build();

        services.AddInfrastructure(configuration);

        ServiceDescriptor publisher = Assert.Single(
            services,
            static service => service.ServiceType == typeof(IPassportAuditPublisher));
        ServiceDescriptor reconciler = Assert.Single(
            services,
            static service => service.ServiceType == typeof(IPassportAuditReconciler));
        ServiceDescriptor contentMutationLeases = Assert.Single(
            services,
            static service => service.ServiceType == typeof(IVisitContentMutationLeaseManager));
        Assert.Equal(ServiceLifetime.Scoped, publisher.Lifetime);
        Assert.Equal(ServiceLifetime.Scoped, reconciler.Lifetime);
        Assert.Equal(typeof(MongoVisitContentMutationLeaseManager), contentMutationLeases.ImplementationType);
        Assert.Equal(ServiceLifetime.Scoped, contentMutationLeases.Lifetime);
        Assert.Contains(
            services,
            static service => service.ServiceType == typeof(IHostedService)
                && service.ImplementationType
                    == typeof(PassportAuditReconciliationBackgroundService));
        Assert.Contains(
            services,
            static service => service.ServiceType == typeof(IHostedService)
                && service.ImplementationType
                    == typeof(VisitDeletionReconciliationBackgroundService));
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
