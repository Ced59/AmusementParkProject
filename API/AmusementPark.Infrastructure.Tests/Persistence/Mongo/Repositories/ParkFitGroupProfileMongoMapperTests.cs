using AmusementPark.Core.Domain.ParkFit;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.ParkFit;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class ParkFitGroupProfileMongoMapperTests
{
    [Fact]
    public void Mapping_ShouldRoundTripOwnerFactsAndVersion()
    {
        DateTime nowUtc = new DateTime(2026, 9, 14, 14, 0, 0, DateTimeKind.Utc);
        ParkFitGroupProfile source = ParkFitGroupProfile.Create(
            ParkFitGroupProfileId.Parse("profile-1"),
            "user-1",
            "Enfant",
            125,
            8,
            true,
            41,
            nowUtc);
        source.Update("Enfant", 126, 9, true, 42, nowUtc.AddMinutes(1));

        ParkFitGroupProfileDocument document = source.ToDocument();
        ParkFitGroupProfile restored = document.ToDomain();

        Assert.Equal(source.Id, restored.Id);
        Assert.Equal(source.OwnerUserId, restored.OwnerUserId);
        Assert.Equal(source.Alias, restored.Alias);
        Assert.Equal(126, restored.HeightCentimeters);
        Assert.Equal(9, restored.AgeYears);
        Assert.Equal(42, restored.CompanionAgeYears);
        Assert.Equal(2, restored.Version);
    }
}
