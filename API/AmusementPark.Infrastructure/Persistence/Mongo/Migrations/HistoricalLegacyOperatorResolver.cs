using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.History;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Migrations;

public sealed class HistoricalLegacyOperatorResolver
{
    private readonly IMongoCollection<ParkOperatorDocument> operatorCollection;

    public HistoricalLegacyOperatorResolver(IMongoDatabase database, MongoDbSettings settings)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(settings);
        this.operatorCollection = database.GetCollection<ParkOperatorDocument>(
            settings.ParkOperatorsCollectionName);
    }

    public async Task<bool> AreReferencesResolvedAsync(
        HistoryEventDocument historyEvent,
        CancellationToken cancellationToken)
    {
        string[] referenceIds = GetReferenceIds(historyEvent);
        if (referenceIds.Length == 0)
        {
            return false;
        }

        long resolvedCount = await this.operatorCollection.CountDocumentsAsync(
            Builders<ParkOperatorDocument>.Filter.In(
                static item => item.Id,
                referenceIds),
            cancellationToken: cancellationToken);
        return resolvedCount == referenceIds.Length;
    }

    internal static string[] GetReferenceIds(HistoryEventDocument historyEvent)
    {
        ArgumentNullException.ThrowIfNull(historyEvent);
        return new[] { historyEvent.PreviousOperatorId, historyEvent.NewOperatorId }
            .Where(static identifier => !string.IsNullOrWhiteSpace(identifier))
            .Select(static identifier => identifier!.Trim())
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static identifier => identifier, StringComparer.Ordinal)
            .ToArray();
    }
}
