using AmusementPark.Application.Features.DataSources.Contracts;
using AmusementPark.Application.Features.DataSources.Results;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Search.Ports;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.CaptainCoaster;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;
using AmusementPark.Infrastructure.Services.DataSources.Acquisition;
using AmusementPark.Infrastructure.Services.DataSources.CaptainCoaster.CaptainCoasterScraping;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Application.Features.Ratings.Models;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using System.Globalization;
using System.Threading.Channels;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AmusementPark.Application.Features.Search;

namespace AmusementPark.Infrastructure.Services.DataSources.CaptainCoaster;

internal static class CaptainCoasterSearchProjectionWorkflow
{
    internal static async Task RefreshSearchProjectionAsync(this CaptainCoasterDataSourceProvider provider,
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
                provider.AddLog(session, "Info", "Aucune entité locale n'a nécessité de rafraîchissement du search index.");
            }

            return;
        }

        try
        {
            if (normalizedParkIds.Count > 0)
            {
                List<string> normalizedParkIdList = normalizedParkIds.ToList();

                await provider.searchProjectionWriter.UpsertManyAsync(
                    SearchProjectionResourceTypes.Parks,
                    normalizedParkIdList,
                    cancellationToken);

                List<string> relatedParkItemIds = await provider.localParkItemsCollection
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
                await provider.searchProjectionWriter.UpsertManyAsync(
                    SearchProjectionResourceTypes.ParkItems,
                    normalizedParkItemIds.ToList(),
                    cancellationToken);
            }

            if (session != null)
            {
                provider.AddLog(
                    session,
                    "Info",
                    $"Index de recherche rafraîchi : {normalizedParkIds.Count} parc(s) et {normalizedParkItemIds.Count} park item(s)."
                );
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            provider.logger.LogWarning(exception, "Unable to refresh Captain Coaster search projection for session {SessionId}.", session?.Id);
            if (session != null)
            {
                provider.AddLog(session, "Warn", $"Échec du rafraîchissement de l'index de recherche : {exception.Message}");
            }
        }
    }
}
