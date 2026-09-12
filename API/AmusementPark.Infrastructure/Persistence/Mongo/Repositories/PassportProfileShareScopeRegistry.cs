using System.Security.Cryptography;
using System.Text;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Sharing;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class PassportProfileShareScopeRegistry : IPassportProfileShareScopeRegistry
{
    private readonly IMongoCollection<PassportProfileShareScopeRegistrationDocument> collection;

    public PassportProfileShareScopeRegistry(
        IMongoDatabase database,
        MongoDbSettings settings)
    {
        this.collection = database.GetCollection<PassportProfileShareScopeRegistrationDocument>(
            settings.PassportProfileShareScopeRegistrationsCollectionName);
    }

    internal PassportProfileShareScopeRegistry(
        IMongoCollection<PassportProfileShareScopeRegistrationDocument> collection)
    {
        this.collection = collection ?? throw new ArgumentNullException(nameof(collection));
    }

    public async Task RegisterAsync(
        string ownerUserId,
        string scopeKey,
        IReadOnlyCollection<int> selectedYears,
        IReadOnlyCollection<string> selectedParkIds,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<PassportProfileShareScopeRegistrationDocument> registrations =
            CreateRegistrations(ownerUserId, scopeKey, selectedYears, selectedParkIds);
        if (registrations.Count == 0)
        {
            return;
        }

        WriteModel<PassportProfileShareScopeRegistrationDocument>[] writes = registrations
            .Select(registration =>
            {
                FilterDefinition<PassportProfileShareScopeRegistrationDocument> filter =
                    Builders<PassportProfileShareScopeRegistrationDocument>.Filter.Eq(
                        document => document.Id,
                        registration.Id);
                UpdateDefinition<PassportProfileShareScopeRegistrationDocument> update =
                    Builders<PassportProfileShareScopeRegistrationDocument>.Update
                        .SetOnInsert(document => document.Id, registration.Id)
                        .Set(document => document.ScopeKey, registration.ScopeKey)
                        .Set(document => document.OwnerUserId, registration.OwnerUserId)
                        .Set(document => document.ParkId, registration.ParkId)
                        .Set(document => document.SelectedYears, registration.SelectedYears);
                return (WriteModel<PassportProfileShareScopeRegistrationDocument>)
                    new UpdateOneModel<PassportProfileShareScopeRegistrationDocument>(
                        filter,
                        update)
                    {
                        IsUpsert = true,
                    };
            })
            .ToArray();

        try
        {
            await this.collection.BulkWriteAsync(
                writes,
                new BulkWriteOptions { IsOrdered = false },
                cancellationToken);
        }
        catch (MongoBulkWriteException<PassportProfileShareScopeRegistrationDocument> exception)
            when (exception.WriteErrors.Count > 0
                && exception.WriteConcernError is null
                && exception.WriteErrors.All(
                    static error => error.Category == ServerErrorCategory.DuplicateKey))
        {
            // A concurrent preview registered the same deterministic dependencies.
        }
    }

    public async Task<IReadOnlyCollection<string>> ResolveScopeKeysAsync(
        string ownerUserId,
        IReadOnlyCollection<(string ParkId, int Year)> segments,
        CancellationToken cancellationToken)
    {
        string normalizedOwnerUserId = NormalizeRequired(ownerUserId, nameof(ownerUserId));
        ArgumentNullException.ThrowIfNull(segments);
        (string ParkId, int Year)[] normalizedSegments = segments
            .Select(static segment => (
                NormalizeRequired(segment.ParkId, nameof(segments)),
                NormalizeYear(segment.Year, nameof(segments))))
            .Distinct()
            .ToArray();
        if (normalizedSegments.Length == 0)
        {
            return Array.Empty<string>();
        }

        FilterDefinitionBuilder<PassportProfileShareScopeRegistrationDocument> filters =
            Builders<PassportProfileShareScopeRegistrationDocument>.Filter;
        FilterDefinition<PassportProfileShareScopeRegistrationDocument>[] segmentFilters =
            normalizedSegments
                .Select(segment =>
                    filters.Eq(document => document.ParkId, segment.ParkId)
                    & filters.AnyEq(document => document.SelectedYears, segment.Year))
                .ToArray();
        FilterDefinition<PassportProfileShareScopeRegistrationDocument> filter =
            filters.Eq(document => document.OwnerUserId, normalizedOwnerUserId)
            & filters.Or(segmentFilters);
        ProjectionDefinition<PassportProfileShareScopeRegistrationDocument, string> projection =
            Builders<PassportProfileShareScopeRegistrationDocument>.Projection.Expression(
                static document => document.ScopeKey);
        List<string> scopeKeys = await this.collection
            .Find(filter)
            .Project(projection)
            .ToListAsync(cancellationToken);
        return scopeKeys
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static value => value, StringComparer.Ordinal)
            .ToArray();
    }

    internal static IReadOnlyCollection<PassportProfileShareScopeRegistrationDocument>
        CreateRegistrations(
            string ownerUserId,
            string scopeKey,
            IReadOnlyCollection<int> selectedYears,
            IReadOnlyCollection<string> selectedParkIds)
    {
        string normalizedOwnerUserId = NormalizeRequired(ownerUserId, nameof(ownerUserId));
        string normalizedScopeKey = NormalizeRequired(scopeKey, nameof(scopeKey));
        ArgumentNullException.ThrowIfNull(selectedYears);
        ArgumentNullException.ThrowIfNull(selectedParkIds);
        List<int> years = selectedYears
            .Select(static year => NormalizeYear(year, nameof(selectedYears)))
            .Distinct()
            .OrderBy(static value => value)
            .ToList();
        string[] parkIds = selectedParkIds
            .Select(static value => NormalizeRequired(value, nameof(selectedParkIds)))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static value => value, StringComparer.Ordinal)
            .ToArray();
        return parkIds
            .Select(parkId => new PassportProfileShareScopeRegistrationDocument
            {
                Id = CreateRegistrationId(normalizedScopeKey, parkId),
                ScopeKey = normalizedScopeKey,
                OwnerUserId = normalizedOwnerUserId,
                ParkId = parkId,
                SelectedYears = new List<int>(years),
            })
            .ToArray();
    }

    internal static string CreateRegistrationId(string scopeKey, string parkId)
    {
        string normalizedScopeKey = NormalizeRequired(scopeKey, nameof(scopeKey));
        string normalizedParkId = NormalizeRequired(parkId, nameof(parkId));
        byte[] canonical = Encoding.UTF8.GetBytes(
            string.Concat(normalizedScopeKey, "\n", normalizedParkId));
        return Convert.ToHexString(SHA256.HashData(canonical)).ToLowerInvariant();
    }

    private static int NormalizeYear(int year, string parameterName)
    {
        return year is >= 1 and <= 9999
            ? year
            : throw new ArgumentOutOfRangeException(parameterName);
    }

    private static string NormalizeRequired(string? value, string parameterName)
    {
        string normalized = value?.Trim() ?? string.Empty;
        return normalized.Length > 0
            ? normalized
            : throw new ArgumentException("A non-empty value is required.", parameterName);
    }
}
