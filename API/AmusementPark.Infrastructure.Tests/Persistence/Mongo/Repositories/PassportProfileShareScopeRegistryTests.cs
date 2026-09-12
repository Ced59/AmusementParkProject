using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Sharing;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class PassportProfileShareScopeRegistryTests
{
    [Fact]
    public void CreateRegistrations_ShouldStoreOneBoundedDocumentPerSelectedPark()
    {
        int[] years = Enumerable.Range(1900, 100).Reverse().Concat(new[] { 1900 }).ToArray();
        string[] parkIds = Enumerable.Range(1, 250)
            .Select(static index => string.Concat("park-", index))
            .Reverse()
            .Concat(new[] { " park-1 " })
            .ToArray();

        PassportProfileShareScopeRegistrationDocument[] registrations =
            PassportProfileShareScopeRegistry.CreateRegistrations(
                    " user-1 ",
                    " scope-1 ",
                    years,
                    parkIds)
                .ToArray();

        Assert.Equal(250, registrations.Length);
        Assert.All(registrations, registration =>
        {
            Assert.Equal("user-1", registration.OwnerUserId);
            Assert.Equal("scope-1", registration.ScopeKey);
            Assert.Equal(100, registration.SelectedYears.Count);
            Assert.Equal(1900, registration.SelectedYears[0]);
            Assert.Equal(1999, registration.SelectedYears[^1]);
        });
        Assert.Equal(250, registrations.Select(static item => item.Id).Distinct().Count());
        Assert.Equal("park-1", registrations[0].ParkId);
    }

    [Fact]
    public void CreateRegistrationId_ShouldBeDeterministicWithoutExposingIdentifiers()
    {
        string first = PassportProfileShareScopeRegistry.CreateRegistrationId(
            "scope-1",
            "park-1");
        string repeated = PassportProfileShareScopeRegistry.CreateRegistrationId(
            " scope-1 ",
            " park-1 ");
        string otherPark = PassportProfileShareScopeRegistry.CreateRegistrationId(
            "scope-1",
            "park-2");

        Assert.Equal(first, repeated);
        Assert.NotEqual(first, otherPark);
        Assert.Equal(64, first.Length);
        Assert.DoesNotContain("scope", first, StringComparison.Ordinal);
        Assert.DoesNotContain("park", first, StringComparison.Ordinal);
    }

    [Fact]
    public void MongoSettings_ShouldUseTheDedicatedCollectionByDefault()
    {
        MongoDbSettings settings = new MongoDbSettings();

        Assert.Equal(
            "passport-profile-share-scope-registrations",
            settings.PassportProfileShareScopeRegistrationsCollectionName);
    }
}
