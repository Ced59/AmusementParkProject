using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Sharing;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Initialization;

internal sealed class PassportProfileShareSelectedParksMigration
{
    private const int BatchSize = 100;
    private const string UnavailableParkName = "Unavailable park";

    private readonly IMongoCollection<PassportProfileShareSnapshotDocument> snapshots;
    private readonly IMongoCollection<ParkDocument> parks;

    public PassportProfileShareSelectedParksMigration(
        IMongoCollection<PassportProfileShareSnapshotDocument> snapshots,
        IMongoCollection<ParkDocument> parks)
    {
        this.snapshots = snapshots ?? throw new ArgumentNullException(nameof(snapshots));
        this.parks = parks ?? throw new ArgumentNullException(nameof(parks));
    }

    public async Task MigrateAsync(CancellationToken cancellationToken)
    {
        FilterDefinition<PassportProfileShareSnapshotDocument> candidateFilter =
            Builders<PassportProfileShareSnapshotDocument>.Filter.Exists(
                "selection.selectedParkIds",
                true)
            & Builders<PassportProfileShareSnapshotDocument>.Filter.Exists(
                "selectedParks",
                false);
        IFindFluent<PassportProfileShareSnapshotDocument, PassportProfileShareSnapshotDocument>
            find = this.snapshots.Find(candidateFilter);
        find.Options.BatchSize = BatchSize;

        using IAsyncCursor<PassportProfileShareSnapshotDocument> cursor =
            await find.ToCursorAsync(cancellationToken);
        while (await cursor.MoveNextAsync(cancellationToken))
        {
            PassportProfileShareSnapshotDocument[] batch = cursor.Current.ToArray();
            await this.MigrateBatchAsync(batch, cancellationToken);
        }
    }

    internal static IReadOnlyCollection<PassportProfileShareSelectedParkDocument>
        BuildSelectedParks(
            IEnumerable<string>? selectedParkIds,
            IEnumerable<PassportProfileShareParkDocument>? frozenParks,
            IReadOnlyDictionary<string, ParkDocument> currentParks)
    {
        ArgumentNullException.ThrowIfNull(currentParks);
        string[] parkIds = (selectedParkIds ?? Array.Empty<string>())
            .Where(static parkId => !string.IsNullOrWhiteSpace(parkId))
            .Select(static parkId => parkId.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        PassportProfileShareParkDocument[] frozen =
            (frozenParks ?? Array.Empty<PassportProfileShareParkDocument>()).ToArray();
        bool[] frozenAssignments = new bool[frozen.Length];
        Dictionary<string, PassportProfileShareSelectedParkDocument> assignments =
            new Dictionary<string, PassportProfileShareSelectedParkDocument>(
                StringComparer.Ordinal);

        for (int frozenIndex = 0; frozenIndex < frozen.Length; frozenIndex++)
        {
            PassportProfileShareParkDocument frozenPark = frozen[frozenIndex];
            string? matchedParkId = parkIds.FirstOrDefault(parkId =>
                !assignments.ContainsKey(parkId)
                && currentParks.TryGetValue(parkId, out ParkDocument? currentPark)
                && HasSameLabel(frozenPark, currentPark));
            if (matchedParkId is null)
            {
                continue;
            }

            assignments[matchedParkId] = CreateSelectedPark(matchedParkId, frozenPark);
            frozenAssignments[frozenIndex] = true;
        }

        Queue<PassportProfileShareParkDocument> unmatchedFrozenParks = new Queue<PassportProfileShareParkDocument>(
            frozen.Where((_, index) => !frozenAssignments[index]));
        foreach (string parkId in parkIds)
        {
            if (assignments.ContainsKey(parkId))
            {
                continue;
            }

            if (unmatchedFrozenParks.TryDequeue(out PassportProfileShareParkDocument? frozenPark))
            {
                assignments[parkId] = CreateSelectedPark(parkId, frozenPark);
                continue;
            }

            assignments[parkId] = currentParks.TryGetValue(parkId, out ParkDocument? currentPark)
                ? CreateSelectedPark(parkId, currentPark.Name, currentPark.CountryCode)
                : CreateSelectedPark(parkId, null, null);
        }

        return parkIds.Select(parkId => assignments[parkId]).ToArray();
    }

    private async Task MigrateBatchAsync(
        IReadOnlyCollection<PassportProfileShareSnapshotDocument> snapshots,
        CancellationToken cancellationToken)
    {
        string[] selectedParkIds = snapshots
            .SelectMany(static snapshot =>
                (IEnumerable<string>?)snapshot.Selection?.SelectedParkIds
                    ?? Array.Empty<string>())
            .Where(static parkId => !string.IsNullOrWhiteSpace(parkId))
            .Select(static parkId => parkId.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        IReadOnlyDictionary<string, ParkDocument> currentParks =
            await this.LoadParksAsync(selectedParkIds, cancellationToken);
        List<WriteModel<PassportProfileShareSnapshotDocument>> writes = snapshots
            .Select(snapshot => new UpdateOneModel<PassportProfileShareSnapshotDocument>(
                Builders<PassportProfileShareSnapshotDocument>.Filter.Eq(
                    static candidate => candidate.Id,
                    snapshot.Id)
                & Builders<PassportProfileShareSnapshotDocument>.Filter.Exists(
                    "selectedParks",
                    false),
                Builders<PassportProfileShareSnapshotDocument>.Update.Set(
                    static candidate => candidate.SelectedParks,
                    BuildSelectedParks(
                            snapshot.Selection?.SelectedParkIds,
                            snapshot.Content?.Parks,
                            currentParks)
                        .ToList())))
            .Cast<WriteModel<PassportProfileShareSnapshotDocument>>()
            .ToList();
        if (writes.Count == 0)
        {
            return;
        }

        await this.snapshots.BulkWriteAsync(
            writes,
            new BulkWriteOptions { IsOrdered = false },
            cancellationToken);
    }

    private async Task<IReadOnlyDictionary<string, ParkDocument>> LoadParksAsync(
        IReadOnlyCollection<string> parkIds,
        CancellationToken cancellationToken)
    {
        if (parkIds.Count == 0)
        {
            return new Dictionary<string, ParkDocument>(StringComparer.Ordinal);
        }

        List<ParkDocument> documents = await this.parks
            .Find(Builders<ParkDocument>.Filter.In(static park => park.Id, parkIds))
            .Project(static park => new ParkDocument
            {
                Id = park.Id,
                Name = park.Name,
                CountryCode = park.CountryCode,
            })
            .ToListAsync(cancellationToken);
        return documents.ToDictionary(static park => park.Id, StringComparer.Ordinal);
    }

    private static bool HasSameLabel(
        PassportProfileShareParkDocument frozenPark,
        ParkDocument currentPark)
    {
        return string.Equals(
                NormalizeName(frozenPark.Name),
                NormalizeName(currentPark.Name),
                StringComparison.OrdinalIgnoreCase)
            && string.Equals(
                NormalizeCountryCode(frozenPark.CountryCode),
                NormalizeCountryCode(currentPark.CountryCode),
                StringComparison.Ordinal);
    }

    private static PassportProfileShareSelectedParkDocument CreateSelectedPark(
        string parkId,
        PassportProfileShareParkDocument frozenPark)
    {
        return CreateSelectedPark(parkId, frozenPark.Name, frozenPark.CountryCode);
    }

    private static PassportProfileShareSelectedParkDocument CreateSelectedPark(
        string parkId,
        string? name,
        string? countryCode)
    {
        return new PassportProfileShareSelectedParkDocument
        {
            ParkId = parkId,
            Name = NormalizeName(name),
            CountryCode = NormalizeCountryCode(countryCode),
        };
    }

    private static string NormalizeName(string? name)
    {
        return string.IsNullOrWhiteSpace(name) ? UnavailableParkName : name.Trim();
    }

    private static string? NormalizeCountryCode(string? countryCode)
    {
        return string.IsNullOrWhiteSpace(countryCode)
            ? null
            : countryCode.Trim().ToUpperInvariant();
    }
}
