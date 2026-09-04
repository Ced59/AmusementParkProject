using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

/// <summary>
/// Projection Mongo limitée à l'identité et aux bornes historiques d'une attraction.
/// </summary>
public sealed class VisitTargetReadRepository : IVisitTargetReadRepository
{
    private readonly IMongoCollection<ParkItemDocument> collection;

    public VisitTargetReadRepository(IMongoDatabase database, MongoDbSettings settings)
    {
        this.collection = database.GetCollection<ParkItemDocument>(
            settings.ParkItemsCollectionName);
    }

    public async Task<IReadOnlyCollection<VisitTarget>> GetByIdsAsync(
        IReadOnlyCollection<string> parkItemIds,
        CancellationToken cancellationToken)
    {
        List<string> normalizedParkItemIds = parkItemIds
            .Where(static parkItemId => !string.IsNullOrWhiteSpace(parkItemId))
            .Select(static parkItemId => parkItemId.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (normalizedParkItemIds.Count == 0)
        {
            return Array.Empty<VisitTarget>();
        }

        FilterDefinition<ParkItemDocument> filter = Builders<ParkItemDocument>.Filter.In(
            document => document.Id,
            normalizedParkItemIds);
        List<ParkItemDocument> documents = await this.collection
            .Find(filter)
            .Project<ParkItemDocument>(BuildProjection())
            .ToListAsync(cancellationToken);
        return documents.Select(static document => new VisitTarget(
                document.Id,
                document.ParkId,
                document.Name,
                document.Category,
                ToDateOnly(document.AttractionDetails?.OpeningDate),
                ToDateOnly(document.AttractionDetails?.ClosingDate),
                NormalizeLifecycleStatus(document.AttractionDetails?.Status),
                document.IsVisible))
            .ToArray();
    }

    internal static ProjectionDefinition<ParkItemDocument> BuildProjection()
    {
        return Builders<ParkItemDocument>.Projection
            .Include(document => document.Id)
            .Include(document => document.ParkId)
            .Include(document => document.Name)
            .Include(document => document.Category)
            .Include(document => document.IsVisible)
            .Include(document => document.AttractionDetails!.OpeningDate)
            .Include(document => document.AttractionDetails!.ClosingDate)
            .Include(document => document.AttractionDetails!.Status);
    }

    private static DateOnly? ToDateOnly(DateTime? value)
    {
        return value.HasValue ? DateOnly.FromDateTime(value.Value) : null;
    }

    internal static string? NormalizeLifecycleStatus(string? value)
    {
        return ParkItemStatusNormalizer.Normalize(value);
    }
}
