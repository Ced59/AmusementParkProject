using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.StandaloneAttractions;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class HistoricalSubjectPublicationStateReader : IHistoricalSubjectPublicationStateReader
{
    private readonly IMongoCollection<ParkDocument> parks;
    private readonly IMongoCollection<ParkItemDocument> parkItems;
    private readonly IMongoCollection<ParkZoneDocument> parkZones;
    private readonly IMongoCollection<StandaloneAttractionDocument> standaloneAttractions;
    private readonly IMongoCollection<ParkOperatorDocument> parkOperators;
    private readonly IMongoCollection<AttractionManufacturerDocument> attractionManufacturers;

    public HistoricalSubjectPublicationStateReader(IMongoDatabase database, MongoDbSettings settings)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(settings);
        this.parks = database.GetCollection<ParkDocument>(settings.ParksCollectionName);
        this.parkItems = database.GetCollection<ParkItemDocument>(settings.ParkItemsCollectionName);
        this.parkZones = database.GetCollection<ParkZoneDocument>(settings.ParkZonesCollectionName);
        this.standaloneAttractions = database.GetCollection<StandaloneAttractionDocument>(
            settings.StandaloneAttractionsCollectionName);
        this.parkOperators = database.GetCollection<ParkOperatorDocument>(settings.ParkOperatorsCollectionName);
        this.attractionManufacturers = database.GetCollection<AttractionManufacturerDocument>(
            settings.AttractionManufacturersCollectionName);
    }

    public async Task<bool> IsPublicAsync(
        HistoricalSubject subject,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(subject);
        if (subject.PublicationPolicy == HistoricalSubjectPublicationPolicy.Suppressed)
        {
            return false;
        }

        if (subject.PublicationPolicy == HistoricalSubjectPublicationPolicy.HistoricalOnly)
        {
            return subject.Type is not HistoricalSubjectType.ParkItem
                    and not HistoricalSubjectType.ParkZone
                || !string.IsNullOrWhiteSpace(subject.ContextParkId)
                    && await this.IsParkPublicAsync(subject.ContextParkId, cancellationToken);
        }

        return subject.Type switch
        {
            HistoricalSubjectType.Park => await this.IsParkPublicAsync(subject.Id, cancellationToken),
            HistoricalSubjectType.ParkItem => await this.IsParkItemPublicAsync(subject, cancellationToken),
            HistoricalSubjectType.StandaloneAttraction =>
                await this.IsStandaloneAttractionPublicAsync(subject.Id, cancellationToken),
            HistoricalSubjectType.ParkZone => await this.IsParkZonePublicAsync(subject, cancellationToken),
            HistoricalSubjectType.ParkOperator => await this.IsParkOperatorPublicAsync(subject.Id, cancellationToken),
            HistoricalSubjectType.AttractionManufacturer =>
                await this.IsAttractionManufacturerPublicAsync(subject.Id, cancellationToken),
            _ => false,
        };
    }

    public async Task<IReadOnlySet<HistoricalSubjectKey>> GetPublicSubjectKeysAsync(
        IReadOnlyCollection<HistoricalSubject> subjects,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(subjects);
        HistoricalSubject[] candidates = subjects
            .Where(static subject => subject.PublicationPolicy == HistoricalSubjectPublicationPolicy.FollowCurrentSubject)
            .DistinctBy(static subject => (subject.Type, subject.Id, subject.ContextParkId))
            .ToArray();
        HashSet<HistoricalSubjectKey> publicKeys = new();
        foreach (IGrouping<HistoricalSubjectType, HistoricalSubject> group in candidates.GroupBy(static subject => subject.Type))
        {
            IReadOnlyCollection<HistoricalSubjectKey> publicGroupKeys = await this.LoadPublicKeysAsync(
                group.Key,
                group.ToArray(),
                cancellationToken);
            publicKeys.UnionWith(publicGroupKeys);
        }

        HistoricalSubject[] historicalParkSubjects = subjects
            .Where(static subject => subject.PublicationPolicy == HistoricalSubjectPublicationPolicy.HistoricalOnly
                && subject.Type is HistoricalSubjectType.ParkItem or HistoricalSubjectType.ParkZone
                && !string.IsNullOrWhiteSpace(subject.ContextParkId))
            .DistinctBy(static subject => (subject.Type, subject.Id, subject.ContextParkId))
            .ToArray();
        IReadOnlyDictionary<string, string> publicContextParks = await this.GetPublicParkNamesAsync(
            historicalParkSubjects.Select(static subject => subject.ContextParkId!).ToArray(),
            cancellationToken);
        publicKeys.UnionWith(historicalParkSubjects
            .Where(subject => publicContextParks.ContainsKey(subject.ContextParkId!))
            .Select(static subject => new HistoricalSubjectKey(
                subject.Type,
                subject.Id,
                subject.ContextParkId)));

        return publicKeys;
    }

    public async Task<IReadOnlyDictionary<string, string>> GetPublicParkNamesAsync(
        IReadOnlyCollection<string> parkIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(parkIds);
        string[] normalizedParkIds = parkIds
            .Where(static parkId => !string.IsNullOrWhiteSpace(parkId))
            .Select(static parkId => parkId.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (normalizedParkIds.Length == 0)
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }

        return (await this.parks.Find(item => normalizedParkIds.Contains(item.Id))
                .ToListAsync(cancellationToken))
            .Where(static park => IsParkPublic(park) && !string.IsNullOrWhiteSpace(park.Name))
            .ToDictionary(static park => park.Id, static park => park.Name!, StringComparer.Ordinal);
    }

    private async Task<IReadOnlyCollection<HistoricalSubjectKey>> LoadPublicKeysAsync(
        HistoricalSubjectType type,
        IReadOnlyCollection<HistoricalSubject> subjects,
        CancellationToken cancellationToken)
    {
        string[] identifiers = subjects
            .Select(static subject => subject.Id)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        return type switch
        {
            HistoricalSubjectType.Park => (await this.parks.Find(item => identifiers.Contains(item.Id))
                    .ToListAsync(cancellationToken))
                .Where(IsParkPublic)
                .Select(static item => new HistoricalSubjectKey(HistoricalSubjectType.Park, item.Id, item.Id))
                .ToArray(),
            HistoricalSubjectType.ParkItem => await this.LoadPublicParkItemKeysAsync(identifiers, cancellationToken),
            HistoricalSubjectType.StandaloneAttraction => BuildPublicKeysPreservingContext(
                HistoricalSubjectType.StandaloneAttraction,
                subjects,
                (await this.standaloneAttractions
                        .Find(item => identifiers.Contains(item.Id)).ToListAsync(cancellationToken))
                    .Where(IsStandaloneAttractionPublic)
                    .Select(static item => item.Id)),
            HistoricalSubjectType.ParkZone => await this.LoadPublicZoneKeysAsync(identifiers, cancellationToken),
            HistoricalSubjectType.ParkOperator => BuildPublicKeysPreservingContext(
                HistoricalSubjectType.ParkOperator,
                subjects,
                (await this.parkOperators
                        .Find(item => identifiers.Contains(item.Id)).ToListAsync(cancellationToken))
                    .Where(static item => item.AdminReviewStatus != AdminReviewStatus.NotRelevant)
                    .Select(static item => item.Id)),
            HistoricalSubjectType.AttractionManufacturer => BuildPublicKeysPreservingContext(
                HistoricalSubjectType.AttractionManufacturer,
                subjects,
                (await this.attractionManufacturers
                        .Find(item => identifiers.Contains(item.Id)).ToListAsync(cancellationToken))
                    .Where(static item => item.IsVisible
                        && item.AdminReviewStatus != AdminReviewStatus.NotRelevant)
                    .Select(static item => item.Id)),
            _ => Array.Empty<HistoricalSubjectKey>(),
        };
    }

    internal static HistoricalSubjectKey[] BuildPublicKeysPreservingContext(
        HistoricalSubjectType type,
        IReadOnlyCollection<HistoricalSubject> candidates,
        IEnumerable<string> publicIdentifiers)
    {
        HashSet<string> publicIdentifierSet = publicIdentifiers.ToHashSet(StringComparer.Ordinal);
        return candidates
            .Where(subject => subject.Type == type && publicIdentifierSet.Contains(subject.Id))
            .Select(static subject => new HistoricalSubjectKey(
                subject.Type,
                subject.Id,
                subject.ContextParkId))
            .Distinct()
            .ToArray();
    }

    private async Task<IReadOnlyCollection<HistoricalSubjectKey>> LoadPublicParkItemKeysAsync(
        IReadOnlyCollection<string> identifiers,
        CancellationToken cancellationToken)
    {
        List<ParkItemDocument> items = await this.parkItems.Find(item => identifiers.Contains(item.Id))
            .ToListAsync(cancellationToken);
        string[] parkIds = items.Select(static item => item.ParkId).Distinct(StringComparer.Ordinal).ToArray();
        HashSet<string> publicParkIds = (await this.parks.Find(item => parkIds.Contains(item.Id))
                .ToListAsync(cancellationToken))
            .Where(IsParkPublic)
            .Select(static item => item.Id)
            .ToHashSet(StringComparer.Ordinal);
        return items.Where(item => item.IsVisible
                && item.AdminReviewStatus != AdminReviewStatus.NotRelevant
                && publicParkIds.Contains(item.ParkId))
            .Select(static item => new HistoricalSubjectKey(
                HistoricalSubjectType.ParkItem,
                item.Id,
                item.ParkId))
            .ToArray();
    }

    private async Task<IReadOnlyCollection<HistoricalSubjectKey>> LoadPublicZoneKeysAsync(
        IReadOnlyCollection<string> identifiers,
        CancellationToken cancellationToken)
    {
        List<ParkZoneDocument> zones = await this.parkZones.Find(item => identifiers.Contains(item.Id))
            .ToListAsync(cancellationToken);
        string[] parkIds = zones.Select(static item => item.ParkId).Distinct(StringComparer.Ordinal).ToArray();
        HashSet<string> publicParkIds = (await this.parks.Find(item => parkIds.Contains(item.Id))
                .ToListAsync(cancellationToken))
            .Where(IsParkPublic)
            .Select(static item => item.Id)
            .ToHashSet(StringComparer.Ordinal);
        return zones.Where(zone => zone.IsVisible && publicParkIds.Contains(zone.ParkId))
            .Select(static zone => new HistoricalSubjectKey(
                HistoricalSubjectType.ParkZone,
                zone.Id,
                zone.ParkId))
            .ToArray();
    }

    private async Task<bool> IsParkPublicAsync(string parkId, CancellationToken cancellationToken)
    {
        ParkDocument? park = await this.parks
            .Find(item => item.Id == parkId)
            .FirstOrDefaultAsync(cancellationToken);
        return IsParkPublic(park);
    }

    private async Task<bool> IsParkItemPublicAsync(
        HistoricalSubject subject,
        CancellationToken cancellationToken)
    {
        ParkItemDocument? parkItem = await this.parkItems
            .Find(item => item.Id == subject.Id)
            .FirstOrDefaultAsync(cancellationToken);
        return parkItem is not null
            && parkItem.IsVisible
            && parkItem.AdminReviewStatus != AdminReviewStatus.NotRelevant
            && (subject.ContextParkId is null
                || string.Equals(subject.ContextParkId, parkItem.ParkId, StringComparison.Ordinal))
            && await this.IsParkPublicAsync(parkItem.ParkId, cancellationToken);
    }

    private async Task<bool> IsStandaloneAttractionPublicAsync(
        string attractionId,
        CancellationToken cancellationToken)
    {
        StandaloneAttractionDocument? attraction = await this.standaloneAttractions
            .Find(item => item.Id == attractionId)
            .FirstOrDefaultAsync(cancellationToken);
        return IsStandaloneAttractionPublic(attraction);
    }

    internal static bool IsStandaloneAttractionPublic(StandaloneAttractionDocument? attraction)
    {
        return attraction is not null
            && attraction.IsVisible
            && attraction.AdminReviewStatus != AdminReviewStatus.NotRelevant
            && !ParkItemStatusNormalizer.IsClosedDefinitively(attraction.AttractionDetails?.Status);
    }

    private async Task<bool> IsParkZonePublicAsync(
        HistoricalSubject subject,
        CancellationToken cancellationToken)
    {
        ParkZoneDocument? zone = await this.parkZones
            .Find(item => item.Id == subject.Id)
            .FirstOrDefaultAsync(cancellationToken);
        return zone is not null
            && zone.IsVisible
            && (subject.ContextParkId is null
                || string.Equals(subject.ContextParkId, zone.ParkId, StringComparison.Ordinal))
            && await this.IsParkPublicAsync(zone.ParkId, cancellationToken);
    }

    private async Task<bool> IsParkOperatorPublicAsync(string operatorId, CancellationToken cancellationToken)
    {
        ParkOperatorDocument? parkOperator = await this.parkOperators
            .Find(item => item.Id == operatorId)
            .FirstOrDefaultAsync(cancellationToken);
        return parkOperator is not null
            && parkOperator.AdminReviewStatus != AdminReviewStatus.NotRelevant;
    }

    private async Task<bool> IsAttractionManufacturerPublicAsync(
        string manufacturerId,
        CancellationToken cancellationToken)
    {
        AttractionManufacturerDocument? manufacturer = await this.attractionManufacturers
            .Find(item => item.Id == manufacturerId)
            .FirstOrDefaultAsync(cancellationToken);
        return manufacturer is not null
            && manufacturer.IsVisible
            && manufacturer.AdminReviewStatus != AdminReviewStatus.NotRelevant;
    }

    private static bool IsParkPublic(ParkDocument? park)
    {
        return park is not null
            && park.IsVisible
            && park.AdminReviewStatus != AdminReviewStatus.NotRelevant
            && park.Status.CanAppearInPublicDiscovery();
    }
}
