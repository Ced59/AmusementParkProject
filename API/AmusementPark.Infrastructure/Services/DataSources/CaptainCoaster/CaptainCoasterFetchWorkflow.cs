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

internal static class CaptainCoasterFetchWorkflow
{
    internal static async Task ProcessCoasterPagesAsync(this CaptainCoasterDataSourceProvider provider,
            CaptainCoasterSyncSessionDocument session,
            IReadOnlyCollection<CaptainCoasterDiscoveredUrl> discoveredUrls,
            CaptainCoasterScrapingSettings scrapingSettings,
            CancellationToken cancellationToken)
    {
        List<CaptainCoasterCoasterSnapshotDocument> existingStagedCoasters = await provider.coastersCollection
            .Find(item => item.SyncSessionId == session.Id)
            .ToListAsync(cancellationToken);

        HashSet<string> existingIds = existingStagedCoasters
            .Select(static item => item.CaptainCoasterId)
            .Where(static item => !string.IsNullOrWhiteSpace(item))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        List<CaptainCoasterDiscoveredUrl> pendingUrls = discoveredUrls
            .Where(item => !existingIds.Contains(item.CaptainCoasterId))
            .ToList();

        int totalCount = discoveredUrls.Count;
        int processedCount = existingIds.Count;
        int skippedCount = totalCount - pendingUrls.Count;
        int failedCount = 0;

        session.Metrics.DiscoveredItems = totalCount;
        session.Metrics.SkippedItems = skippedCount;
        session.Metrics.ProcessedItems = processedCount;
        session.Metrics.FailedItems = failedCount;
        session.Metrics.CoastersFetched = processedCount;
        session.ProgressPercentage = provider.CalculateFetchProgress(processedCount + skippedCount + failedCount, totalCount);
        session.Message = $"Pages coaster traitées : {processedCount}/{totalCount}.";
        session.UpdatedAt = DateTime.UtcNow;
        await provider.PersistSessionAsync(session, cancellationToken);

        int maxConcurrentRequests = provider.NormalizePositiveBounded(scrapingSettings.MaxConcurrentRequests, 4, 1, 16);
        int writeBatchSize = provider.NormalizePositiveBounded(scrapingSettings.CoasterWriteBatchSize, 50, 5, 500);
        int progressSaveInterval = provider.NormalizePositiveBounded(scrapingSettings.ProgressSaveInterval, 25, 1, 500);

        Channel<CaptainCoasterFetchOutcome> channel = Channel.CreateBounded<CaptainCoasterFetchOutcome>(new BoundedChannelOptions(writeBatchSize * Math.Max(2, maxConcurrentRequests))
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.Wait,
        });

        Task producerTask = Task.Run(async () =>
        {
            try
            {
                await Parallel.ForEachAsync(
                    pendingUrls,
                    new ParallelOptions
                    {
                        CancellationToken = cancellationToken,
                        MaxDegreeOfParallelism = maxConcurrentRequests,
                    },
                    async (discoveredUrl, ct) =>
                    {
                        try
                        {
                            string html = await provider.dataAcquisitionHttpFetcher.GetStringAsync(
                                discoveredUrl.Url,
                                scrapingSettings.Language + ";q=1.0,en;q=0.8",
                                provider.BuildRequestOptions(scrapingSettings),
                                ct);

                            CaptainCoasterParsedCoaster parsed = provider.coasterPageParser.Parse(discoveredUrl, html, scrapingSettings);
                            CaptainCoasterCoasterSnapshotDocument document = provider.MapParsedCoaster(session.Id, parsed);
                            await channel.Writer.WriteAsync(CaptainCoasterFetchOutcome.Success(document), ct);
                        }
                        catch (OperationCanceledException) when (ct.IsCancellationRequested)
                        {
                            throw;
                        }
                        catch (Exception exception)
                        {
                            provider.logger.LogWarning(exception, "Unable to fetch Captain Coaster URL {Url} for session {SessionId}.", discoveredUrl.Url, session.Id);
                            await channel.Writer.WriteAsync(CaptainCoasterFetchOutcome.Failure(discoveredUrl, exception.Message), ct);
                        }
                    });
            }
            finally
            {
                channel.Writer.TryComplete();
            }
        }, cancellationToken);

        List<CaptainCoasterCoasterSnapshotDocument> writeBuffer = new List<CaptainCoasterCoasterSnapshotDocument>(writeBatchSize);
        int processedSinceLastSave = 0;

        await foreach (CaptainCoasterFetchOutcome outcome in channel.Reader.ReadAllAsync(cancellationToken))
        {
            if (outcome.Document is not null)
            {
                writeBuffer.Add(outcome.Document);
                processedCount++;
                processedSinceLastSave++;

                if (writeBuffer.Count >= writeBatchSize)
                {
                    await provider.BulkUpsertCoastersAsync(session.Id, writeBuffer, cancellationToken);
                    writeBuffer.Clear();
                }
            }
            else
            {
                failedCount++;
                provider.AddLog(session, "Error", $"Échec sur {outcome.DiscoveredUrl?.Url}: {outcome.ErrorMessage}");
            }

            session.Metrics.ProcessedItems = processedCount;
            session.Metrics.FailedItems = failedCount;
            session.Metrics.SkippedItems = skippedCount;
            session.Metrics.CoastersFetched = processedCount;
            session.ProgressPercentage = provider.CalculateFetchProgress(processedCount + skippedCount + failedCount, totalCount);
            session.Message = $"Pages coaster traitées : {processedCount}/{totalCount}.";
            session.UpdatedAt = DateTime.UtcNow;

            if (processedSinceLastSave >= progressSaveInterval || failedCount > 0 && (processedCount + failedCount) % progressSaveInterval == 0)
            {
                provider.AddLog(session, "Info", session.Message);
                await provider.PersistSessionAsync(session, cancellationToken);
                processedSinceLastSave = 0;
            }
        }

        await producerTask;

        if (writeBuffer.Count > 0)
        {
            await provider.BulkUpsertCoastersAsync(session.Id, writeBuffer, cancellationToken);
            writeBuffer.Clear();
        }

        List<CaptainCoasterCoasterSnapshotDocument> stagedCoasters = await provider.coastersCollection
            .Find(item => item.SyncSessionId == session.Id)
            .ToListAsync(cancellationToken);

        List<CaptainCoasterParkSnapshotDocument> stagedParks = provider.BuildParkSnapshots(session.Id, stagedCoasters);
        await provider.parksCollection.DeleteManyAsync(item => item.SyncSessionId == session.Id, cancellationToken);
        if (stagedParks.Count > 0)
        {
            await provider.parksCollection.InsertManyAsync(stagedParks, cancellationToken: cancellationToken);
        }

        session.Metrics.CoastersFetched = stagedCoasters.Count;
        session.Metrics.ParksFetched = stagedParks.Count;
        session.LastCompletedStep = "FetchCoasters";
        session.UpdatedAt = DateTime.UtcNow;
        provider.AddLog(session, "Info", $"Staging reconstruit : {stagedParks.Count} parc(s), {stagedCoasters.Count} coaster(s).");
        await provider.PersistSessionAsync(session, cancellationToken);
    }

    internal static async Task BulkUpsertCoastersAsync(this CaptainCoasterDataSourceProvider provider,
        string sessionId,
        IReadOnlyCollection<CaptainCoasterCoasterSnapshotDocument> documents,
        CancellationToken cancellationToken)
    {
        if (documents.Count == 0)
        {
            return;
        }

        List<WriteModel<CaptainCoasterCoasterSnapshotDocument>> operations = documents
            .Select(document =>
                (WriteModel<CaptainCoasterCoasterSnapshotDocument>)new ReplaceOneModel<CaptainCoasterCoasterSnapshotDocument>(
                    Builders<CaptainCoasterCoasterSnapshotDocument>.Filter.Where(item => item.SyncSessionId == sessionId && item.CaptainCoasterId == document.CaptainCoasterId),
                    document)
                {
                    IsUpsert = true,
                })
            .ToList();

        await provider.coastersCollection.BulkWriteAsync(operations, new BulkWriteOptions { IsOrdered = false }, cancellationToken);
    }



    // -----------------------------------------------------------------------
    // Session helpers
    // -----------------------------------------------------------------------
}
