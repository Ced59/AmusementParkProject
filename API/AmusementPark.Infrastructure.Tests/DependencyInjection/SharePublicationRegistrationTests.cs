using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.DependencyInjection;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using AmusementPark.Infrastructure.Services.Sharing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.DependencyInjection;

public sealed class SharePublicationRegistrationTests
{
    [Fact]
    public void MongoSettings_ShouldUseTheDedicatedSharePublicationCollection()
    {
        MongoDbSettings settings = new MongoDbSettings();

        Assert.Equal("share-publications", settings.SharePublicationsCollectionName);
    }

    [Fact]
    public void AddInfrastructure_ShouldRegisterTheSharingPortsWithExpectedLifetimes()
    {
        ServiceCollection services = new ServiceCollection();
        IConfiguration configuration = new ConfigurationBuilder().Build();

        services.AddInfrastructure(configuration);

        ServiceDescriptor repository = Assert.Single(
            services,
            static service => service.ServiceType == typeof(ISharePublicationRepository));
        Assert.Equal(typeof(SharePublicationRepository), repository.ImplementationType);
        Assert.Equal(ServiceLifetime.Scoped, repository.Lifetime);
        ServiceDescriptor tokenFactory = Assert.Single(
            services,
            static service => service.ServiceType == typeof(IShareTokenFactory));
        Assert.Equal(typeof(CryptographicShareTokenFactory), tokenFactory.ImplementationType);
        Assert.Equal(ServiceLifetime.Singleton, tokenFactory.Lifetime);
    }
}
