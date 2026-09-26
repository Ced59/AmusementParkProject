using AmusementPark.Core.Domain.History;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.History;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Mappers;

public sealed class HistoryEventMongoMapperTests
{
    [Fact]
    public void CanonicalMetadata_ShouldRoundTripWithoutEnteringPublicContracts()
    {
        Guid factId = Guid.NewGuid();
        HistoryEventDocument document = new HistoryEventDocument
        {
            Id = "event-1",
            CanonicalFactId = factId.ToString("N"),
            CanonicalizationState = HistoricalNarrativeCanonicalizationState.Migrated,
        };

        HistoryEvent domain = document.ToDomain();
        HistoryEventDocument roundTrip = domain.ToDocument();

        Assert.Equal(factId, domain.CanonicalFactId);
        Assert.Equal(HistoricalNarrativeCanonicalizationState.Migrated, domain.CanonicalizationState);
        Assert.Equal(document.CanonicalFactId, roundTrip.CanonicalFactId);
        Assert.Equal(document.CanonicalizationState, roundTrip.CanonicalizationState);
    }
}
