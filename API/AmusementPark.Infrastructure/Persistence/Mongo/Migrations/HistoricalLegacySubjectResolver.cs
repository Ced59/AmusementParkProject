using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.History;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.StandaloneAttractions;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Migrations;

public sealed class HistoricalLegacySubjectResolver
{
    private readonly IMongoCollection<ParkDocument> parkCollection;
    private readonly IMongoCollection<ParkItemDocument> parkItemCollection;
    private readonly IMongoCollection<StandaloneAttractionDocument> standaloneCollection;
    private readonly Dictionary<string, HistoricalLegacySubjectResolution> cache =
        new Dictionary<string, HistoricalLegacySubjectResolution>(StringComparer.Ordinal);

    public HistoricalLegacySubjectResolver(IMongoDatabase database, MongoDbSettings settings)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(settings);
        this.parkCollection = database.GetCollection<ParkDocument>(settings.ParksCollectionName);
        this.parkItemCollection = database.GetCollection<ParkItemDocument>(settings.ParkItemsCollectionName);
        this.standaloneCollection = database.GetCollection<StandaloneAttractionDocument>(
            settings.StandaloneAttractionsCollectionName);
    }

    public async Task<HistoricalLegacySubjectResolution> ResolveAsync(
        HistoryEventDocument historyEvent,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(historyEvent);
        string cacheKey = string.Concat(historyEvent.EntityType, ":", historyEvent.OwnerId);
        if (this.cache.TryGetValue(cacheKey, out HistoricalLegacySubjectResolution? cached))
        {
            return cached;
        }

        HistoricalLegacySubjectResolution resolution = await this.ResolveCurrentAsync(
            historyEvent,
            cancellationToken);
        this.cache[cacheKey] = resolution;
        return resolution;
    }

    public async Task<HistoricalLegacySubjectResolution> ResolveCurrentAsync(
        HistoryEventDocument historyEvent,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(historyEvent);
        HistoricalLegacySubjectResolution resolution;
        if (historyEvent.EntityType == HistoryEntityType.Park)
        {
            ParkDocument? park = await this.parkCollection.Find(item => item.Id == historyEvent.OwnerId)
                .FirstOrDefaultAsync(cancellationToken);
            resolution = Resolve(
                HistoricalSubjectType.Park,
                historyEvent.OwnerId,
                park?.Name,
                park is null
                    ? null
                    : park.IsVisible
                        && park.AdminReviewStatus != AdminReviewStatus.NotRelevant
                        && park.Status.CanAppearInPublicDiscovery(),
                park?.AdminReviewStatus);
        }
        else if (historyEvent.EntityType == HistoryEntityType.ParkItem)
        {
            ParkItemDocument? item = await this.parkItemCollection
                .Find(candidate => candidate.Id == historyEvent.OwnerId)
                .FirstOrDefaultAsync(cancellationToken);
            bool? isVisible = item?.IsVisible;
            AdminReviewStatus? reviewStatus = item?.AdminReviewStatus;
            if (item is not null)
            {
                ParkDocument? parent = await this.parkCollection.Find(park => park.Id == item.ParkId)
                    .FirstOrDefaultAsync(cancellationToken);
                if (parent is null
                    || !parent.IsVisible
                    || parent.AdminReviewStatus == AdminReviewStatus.NotRelevant
                    || !parent.Status.CanAppearInPublicDiscovery())
                {
                    isVisible = false;
                    reviewStatus = parent?.AdminReviewStatus;
                }
            }

            resolution = Resolve(
                HistoricalSubjectType.ParkItem,
                historyEvent.OwnerId,
                item?.Name,
                isVisible,
                reviewStatus);
        }
        else if (historyEvent.EntityType == HistoryEntityType.StandaloneAttraction)
        {
            StandaloneAttractionDocument? standalone = await this.standaloneCollection
                .Find(item => item.Id == historyEvent.OwnerId)
                .FirstOrDefaultAsync(cancellationToken);
            resolution = Resolve(
                HistoricalSubjectType.StandaloneAttraction,
                historyEvent.OwnerId,
                standalone?.Name,
                standalone is null
                    ? null
                    : HistoricalSubjectPublicationStateReader.IsStandaloneAttractionPublic(standalone),
                standalone?.AdminReviewStatus);
        }
        else
        {
            throw new HistoricalPersistenceValidationException(
                HistoricalPersistenceErrorCodes.InvalidEnum,
                "A legacy historical subject type cannot be inferred.");
        }

        return resolution;
    }

    private static HistoricalLegacySubjectResolution Resolve(
        HistoricalSubjectType subjectType,
        string subjectId,
        string? label,
        bool? isVisible,
        AdminReviewStatus? reviewStatus)
    {
        if (!isVisible.HasValue)
        {
            return new HistoricalLegacySubjectResolution(
                subjectType,
                subjectId,
                label ?? "Cible historique introuvable",
                HistoricalSubjectPublicationPolicy.Suppressed,
                HistoricalLegacyMigrationAnomalyCodes.MissingSubject);
        }

        if (reviewStatus == AdminReviewStatus.NotRelevant)
        {
            return new HistoricalLegacySubjectResolution(
                subjectType,
                subjectId,
                label ?? "Cible historique sans libellé",
                HistoricalSubjectPublicationPolicy.Suppressed,
                HistoricalLegacyMigrationAnomalyCodes.NotRelevantSubject);
        }

        return new HistoricalLegacySubjectResolution(
            subjectType,
            subjectId,
            label ?? "Cible historique sans libellé",
            isVisible.Value
                ? HistoricalSubjectPublicationPolicy.FollowCurrentSubject
                : HistoricalSubjectPublicationPolicy.Suppressed,
            isVisible.Value ? null : HistoricalLegacyMigrationAnomalyCodes.HiddenSubject);
    }
}
