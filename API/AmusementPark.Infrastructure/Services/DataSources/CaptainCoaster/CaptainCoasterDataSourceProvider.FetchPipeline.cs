using System.Threading.Channels;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.CaptainCoaster;
using AmusementPark.Infrastructure.Services.DataSources.CaptainCoaster.CaptainCoasterScraping;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Services.DataSources.CaptainCoaster;

internal sealed partial class CaptainCoasterDataSourceProvider
{
    private async Task ProcessCoasterPagesAsync(
        CaptainCoasterSyncSessionDocument session,
        IReadOnlyCollection<CaptainCoasterDiscoveredUrl> discoveredUrls,
        CaptainCoasterScrapingSettings scrapingSettings,
        CancellationToken cancellationToken)
    {
        List<CaptainCoasterCoasterSnapshotDocument> existingStagedCoasters = await this.coastersCollection
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
        session.ProgressPercentage = CalculateFetchProgress(processedCount + skippedCount + failedCount, totalCount);
        session.Message = $"Pages coaster traitées : {processedCount}/{totalCount}.";
        session.UpdatedAt = DateTime.UtcNow;
        await this.PersistSessionAsync(session, cancellationToken);

        int maxConcurrentRequests = NormalizePositiveBounded(scrapingSettings.MaxConcurrentRequests, 4, 1, 16);
        int writeBatchSize = NormalizePositiveBounded(scrapingSettings.CoasterWriteBatchSize, 50, 5, 500);
        int progressSaveInterval = NormalizePositiveBounded(scrapingSettings.ProgressSaveInterval, 25, 1, 500);

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
                            string html = await this.dataAcquisitionHttpFetcher.GetStringAsync(
                                discoveredUrl.Url,
                                scrapingSettings.Language + ";q=1.0,en;q=0.8",
                                BuildRequestOptions(scrapingSettings),
                                ct);

                            CaptainCoasterParsedCoaster parsed = this.coasterPageParser.Parse(discoveredUrl, html, scrapingSettings);
                            CaptainCoasterCoasterSnapshotDocument document = MapParsedCoaster(session.Id, parsed);
                            await channel.Writer.WriteAsync(CaptainCoasterFetchOutcome.Success(document), ct);
                        }
                        catch (OperationCanceledException) when (ct.IsCancellationRequested)
                        {
                            throw;
                        }
                        catch (Exception exception)
                        {
                            this.logger.LogWarning(exception, "Unable to fetch Captain Coaster URL {Url} for session {SessionId}.", discoveredUrl.Url, session.Id);
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
                    await this.BulkUpsertCoastersAsync(session.Id, writeBuffer, cancellationToken);
                    writeBuffer.Clear();
                }
            }
            else
            {
                failedCount++;
                AddLog(session, "Error", $"Échec sur {outcome.DiscoveredUrl?.Url}: {outcome.ErrorMessage}");
            }

            session.Metrics.ProcessedItems = processedCount;
            session.Metrics.FailedItems = failedCount;
            session.Metrics.SkippedItems = skippedCount;
            session.Metrics.CoastersFetched = processedCount;
            session.ProgressPercentage = CalculateFetchProgress(processedCount + skippedCount + failedCount, totalCount);
            session.Message = $"Pages coaster traitées : {processedCount}/{totalCount}.";
            session.UpdatedAt = DateTime.UtcNow;

            if (processedSinceLastSave >= progressSaveInterval || failedCount > 0 && (processedCount + failedCount) % progressSaveInterval == 0)
            {
                AddLog(session, "Info", session.Message);
                await this.PersistSessionAsync(session, cancellationToken);
                processedSinceLastSave = 0;
            }
        }

        await producerTask;

        if (writeBuffer.Count > 0)
        {
            await this.BulkUpsertCoastersAsync(session.Id, writeBuffer, cancellationToken);
            writeBuffer.Clear();
        }

        List<CaptainCoasterCoasterSnapshotDocument> stagedCoasters = await this.coastersCollection
            .Find(item => item.SyncSessionId == session.Id)
            .ToListAsync(cancellationToken);

        List<CaptainCoasterParkSnapshotDocument> stagedParks = BuildParkSnapshots(session.Id, stagedCoasters);
        await this.parksCollection.DeleteManyAsync(item => item.SyncSessionId == session.Id, cancellationToken);
        if (stagedParks.Count > 0)
        {
            await this.parksCollection.InsertManyAsync(stagedParks, cancellationToken: cancellationToken);
        }

        session.Metrics.CoastersFetched = stagedCoasters.Count;
        session.Metrics.ParksFetched = stagedParks.Count;
        session.LastCompletedStep = "FetchCoasters";
        session.UpdatedAt = DateTime.UtcNow;
        AddLog(session, "Info", $"Staging reconstruit : {stagedParks.Count} parc(s), {stagedCoasters.Count} coaster(s).");
        await this.PersistSessionAsync(session, cancellationToken);
    }

    private async Task BulkUpsertCoastersAsync(
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

        await this.coastersCollection.BulkWriteAsync(operations, new BulkWriteOptions { IsOrdered = false }, cancellationToken);
    }

    private sealed record CaptainCoasterFetchOutcome(
        CaptainCoasterDiscoveredUrl? DiscoveredUrl,
        CaptainCoasterCoasterSnapshotDocument? Document,
        string? ErrorMessage)
    {
        public static CaptainCoasterFetchOutcome Success(CaptainCoasterCoasterSnapshotDocument document)
        {
            return new CaptainCoasterFetchOutcome(null, document, null);
        }

        public static CaptainCoasterFetchOutcome Failure(CaptainCoasterDiscoveredUrl discoveredUrl, string errorMessage)
        {
            return new CaptainCoasterFetchOutcome(discoveredUrl, null, errorMessage);
        }
    }
}
