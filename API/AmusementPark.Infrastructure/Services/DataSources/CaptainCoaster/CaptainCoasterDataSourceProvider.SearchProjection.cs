using AmusementPark.Application.Features.Search;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.CaptainCoaster;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Services.DataSources;

internal sealed partial class CaptainCoasterDataSourceProvider : IDataSourceProvider, IDataSourceImportExecutor
{

    private async Task RefreshSearchProjectionAsync(
        CaptainCoasterSyncSessionDocument? session,
        IReadOnlyCollection<string> parkIds,
        IReadOnlyCollection<string> parkItemIds,
        CancellationToken cancellationToken)
    {
        HashSet<string> normalizedParkIds = parkIds
            .Where(static item => !string.IsNullOrWhiteSpace(item))
            .Select(static item => item.Trim())
            .ToHashSet(StringComparer.Ordinal);

        HashSet<string> normalizedParkItemIds = parkItemIds
            .Where(static item => !string.IsNullOrWhiteSpace(item))
            .Select(static item => item.Trim())
            .ToHashSet(StringComparer.Ordinal);

        if (normalizedParkIds.Count == 0 && normalizedParkItemIds.Count == 0)
        {
            if (session != null)
            {
                AddLog(session, "Info", "Aucune entité locale n'a nécessité de rafraîchissement du search index.");
            }

            return;
        }

        try
        {
            if (normalizedParkIds.Count > 0)
            {
                List<string> normalizedParkIdList = normalizedParkIds.ToList();

                await this.searchProjectionWriter.UpsertManyAsync(
                    SearchProjectionResourceTypes.Parks,
                    normalizedParkIdList,
                    cancellationToken);

                List<string> relatedParkItemIds = await this.localParkItemsCollection
                    .Find(item => normalizedParkIdList.Contains(item.ParkId))
                    .Project(item => item.Id)
                    .ToListAsync(cancellationToken);

                foreach (string relatedParkItemId in relatedParkItemIds)
                {
                    if (!string.IsNullOrWhiteSpace(relatedParkItemId))
                    {
                        normalizedParkItemIds.Add(relatedParkItemId.Trim());
                    }
                }
            }

            if (normalizedParkItemIds.Count > 0)
            {
                await this.searchProjectionWriter.UpsertManyAsync(
                    SearchProjectionResourceTypes.ParkItems,
                    normalizedParkItemIds.ToList(),
                    cancellationToken);
            }

            if (session != null)
            {
                AddLog(
                    session,
                    "Info",
                    $"Index de recherche rafraîchi : {normalizedParkIds.Count} parc(s) et {normalizedParkItemIds.Count} park item(s)."
                );
            }
        }
        catch (Exception exception)
        {
            if (session != null)
            {
                AddLog(session, "Warn", $"Échec du rafraîchissement de l'index de recherche : {exception.Message}");
            }
        }
    }

    private sealed class CaptainCoasterApplyImpact
    {
        public bool Applied { get; init; }

        public string? ParkId { get; init; }

        public string? ParkItemId { get; init; }
    }
}
