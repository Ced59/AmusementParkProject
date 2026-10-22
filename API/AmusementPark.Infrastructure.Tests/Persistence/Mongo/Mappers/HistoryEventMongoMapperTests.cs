using AmusementPark.Core.Domain.History;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.History;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Mappers;

public sealed class HistoryEventMongoMapperTests
{
    [Fact]
    public void OwnerNames_ShouldRoundTripSeparatelyFromOperatorIds()
    {
        HistoryEvent source = new HistoryEvent
        {
            PreviousOwnerName = "Founding family",
            NewOwnerName = "Purchasing group",
            PreviousOperatorId = "previous-operator",
            NewOperatorId = "current-operator",
        };

        HistoryEvent roundTrip = source.ToDocument().ToDomain();

        Assert.Equal(source.PreviousOwnerName, roundTrip.PreviousOwnerName);
        Assert.Equal(source.NewOwnerName, roundTrip.NewOwnerName);
        Assert.Equal(source.PreviousOperatorId, roundTrip.PreviousOperatorId);
        Assert.Equal(source.NewOperatorId, roundTrip.NewOperatorId);
    }

    [Fact]
    public void CanonicalMetadata_ShouldRoundTripWithoutEnteringPublicContracts()
    {
        Guid factId = Guid.NewGuid();
        HistoryEventDocument document = new HistoryEventDocument
        {
            Id = "event-1",
            CanonicalFactId = factId.ToString("N"),
            CanonicalizationState = HistoricalNarrativeCanonicalizationState.Canonicalized,
        };

        HistoryEvent domain = document.ToDomain();
        HistoryEventDocument roundTrip = domain.ToDocument();

        Assert.Equal(factId, domain.CanonicalFactId);
        Assert.Equal(HistoricalNarrativeCanonicalizationState.Canonicalized, domain.CanonicalizationState);
        Assert.Equal(document.CanonicalFactId, roundTrip.CanonicalFactId);
        Assert.Equal(document.CanonicalizationState, roundTrip.CanonicalizationState);
    }
}
