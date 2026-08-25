using AmusementPark.Application.Common.Requests;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

internal static class ParkItemClosedEntityMongoFilter
{
    private const string ClosedStatusPattern = "^(temporarily\\s*closed|temporarily-closed|temporarily_closed|temporarilyclosed|temporary\\s*closed|temporary-closed|temporary_closed|temporaryclosed|closed\\s*temporarily|closed-temporarily|closed_temporarily|closedtemporarily|closed\\s*definitively|closed-definitively|closed_definitively|closeddefinitively|permanently\\s*closed|permanently-closed|permanently_closed|permanentlyclosed|definitively\\s*closed|definitively-closed|definitively_closed|definitivelyclosed|ferme\\s*definitivement|fermé\\s*définitivement|fermedefinitivement|removed|dismantled)$";

    public static FilterDefinition<ParkItemDocument> Build(ClosedEntityFilter closedFilter)
    {
        FilterDefinition<ParkItemDocument> closedFilterDefinition = Builders<ParkItemDocument>.Filter.Regex(
            "attractionDetails.status",
            new BsonRegularExpression(ClosedStatusPattern, "i"));

        return closedFilter switch
        {
            ClosedEntityFilter.All => Builders<ParkItemDocument>.Filter.Empty,
            ClosedEntityFilter.ClosedOnly => closedFilterDefinition,
            _ => Builders<ParkItemDocument>.Filter.Not(closedFilterDefinition),
        };
    }
}
