using System.Text.Json;
using AmusementPark.Application.Features.BackgroundJobs.Models;
using AmusementPark.Application.Features.BackgroundJobs.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Services;

public sealed class PassportExportJobHandler : IDurableBackgroundJobHandler
{
    private const int MaximumAttempts = 5;
    private readonly IPassportExportRepository exportRepository;
    private readonly IUserVisitRepository visitRepository;
    private readonly IRideOccurrenceRepository occurrenceRepository;
    private readonly IParkRepository parkRepository;
    private readonly IVisitTargetResolver targetResolver;
    private readonly IVisitExportWriter writer;
    private readonly IPassportClock clock;

    public PassportExportJobHandler(
        IPassportExportRepository exportRepository,
        IUserVisitRepository visitRepository,
        IRideOccurrenceRepository occurrenceRepository,
        IParkRepository parkRepository,
        IVisitTargetResolver targetResolver,
        IVisitExportWriter writer,
        IPassportClock clock)
    {
        this.exportRepository = exportRepository;
        this.visitRepository = visitRepository;
        this.occurrenceRepository = occurrenceRepository;
        this.parkRepository = parkRepository;
        this.targetResolver = targetResolver;
        this.writer = writer;
        this.clock = clock;
    }

    public DurableBackgroundJobHandlerDefinition Definition { get; } =
        new DurableBackgroundJobHandlerDefinition(
            PassportExportJob.Kind,
            DurableBackgroundJobWorkload.Heavy,
            new[] { PassportExportJob.PayloadVersion },
            TimeSpan.FromMinutes(10),
            MaximumAttempts,
            TimeSpan.FromSeconds(30),
            TimeSpan.FromMinutes(10),
            maximumConcurrency: 1);

    public async Task<DurableBackgroundJobHandlerResult> HandleAsync(
        DurableBackgroundJobExecutionContext context,
        CancellationToken cancellationToken)
    {
        PassportExportJobPayload? payload = Deserialize(context);
        if (payload is null)
        {
            return DurableBackgroundJobHandlerResult.DeadLetter(
                PassportExportErrorCodes.InvalidPayload);
        }

        PassportExport? passportExport = await this.exportRepository.GetOwnedAsync(
            payload.ExportId,
            payload.UserId,
            cancellationToken);
        if (passportExport is null)
        {
            return DurableBackgroundJobHandlerResult.DeadLetter(
                PassportExportErrorCodes.ExportNotFound);
        }

        if (passportExport.Status is PassportExportStatus.Ready
            or PassportExportStatus.Failed
            || passportExport.ExpiresAtUtc <= this.clock.UtcNow)
        {
            return DurableBackgroundJobHandlerResult.Success();
        }

        bool markedProcessing = await this.exportRepository.TryMarkProcessingAsync(
            passportExport.Id,
            passportExport.UserId,
            this.clock.UtcNow,
            cancellationToken);
        if (!markedProcessing)
        {
            return DurableBackgroundJobHandlerResult.Retry(
                PassportExportErrorCodes.PersistenceConflict);
        }

        try
        {
            PassportExportWriteRequest request = await this.BuildWriteRequestAsync(
                passportExport,
                cancellationToken);
            PassportExportArtifact artifact = this.writer.Write(request);
            DateTime completedAtUtc = this.clock.UtcNow;
            bool completed = await this.exportRepository.TryCompleteAsync(
                passportExport.Id,
                passportExport.UserId,
                artifact,
                completedAtUtc,
                completedAtUtc.Add(PassportExportJob.Retention),
                cancellationToken);
            return completed
                ? DurableBackgroundJobHandlerResult.Success()
                : DurableBackgroundJobHandlerResult.Retry(
                    PassportExportErrorCodes.PersistenceConflict);
        }
        catch (PassportExportSizeLimitException)
        {
            await this.exportRepository.TryFailAsync(
                passportExport.Id,
                passportExport.UserId,
                PassportExportErrorCodes.TooLarge,
                this.clock.UtcNow,
                cancellationToken);
            return DurableBackgroundJobHandlerResult.DeadLetter(
                PassportExportErrorCodes.TooLarge);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            if (context.AttemptCount < MaximumAttempts)
            {
                return DurableBackgroundJobHandlerResult.Retry(
                    PassportExportErrorCodes.GenerationFailed);
            }

            await this.exportRepository.TryFailAsync(
                passportExport.Id,
                passportExport.UserId,
                PassportExportErrorCodes.GenerationFailed,
                this.clock.UtcNow,
                cancellationToken);
            return DurableBackgroundJobHandlerResult.DeadLetter(
                PassportExportErrorCodes.GenerationFailed);
        }
    }

    private async Task<PassportExportWriteRequest> BuildWriteRequestAsync(
        PassportExport passportExport,
        CancellationToken cancellationToken)
    {
        PassportExportSourceBudget sourceBudget = new PassportExportSourceBudget(
            PassportExportJob.MaximumSourceBytes);
        IReadOnlyCollection<Visit> visits =
            await this.visitRepository.ListAllOwnedForExportAsync(
                passportExport.UserId,
                sourceBudget,
                cancellationToken);
        IReadOnlyCollection<RideOccurrence> loadedOccurrences =
            await this.occurrenceRepository.ListAllOwnedForExportAsync(
                passportExport.UserId,
                visits.Select(static visit => visit.Id).ToArray(),
                sourceBudget,
                cancellationToken);
        string[] parkIds = visits.Select(static visit => visit.ParkId)
            .Concat(loadedOccurrences.Select(static occurrence => occurrence.ParkId))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        string[] parkItemIds = loadedOccurrences
            .Select(static occurrence => occurrence.ParkItemId)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        Task<IReadOnlyCollection<Park>> parksTask =
            this.parkRepository.GetByIdsAsync(parkIds, cancellationToken);
        Task<IReadOnlyDictionary<string, VisitTarget>> targetsTask =
            this.targetResolver.ResolveAsync(parkItemIds, cancellationToken);
        await Task.WhenAll(parksTask, targetsTask);

        IReadOnlyDictionary<string, Park> parks = (await parksTask).ToDictionary(
            static park => park.Id,
            StringComparer.Ordinal);
        return new PassportExportWriteRequest(
            passportExport.Id,
            passportExport.Format,
            this.clock.UtcNow,
            visits,
            loadedOccurrences,
            parks,
            await targetsTask);
    }

    private static PassportExportJobPayload? Deserialize(
        DurableBackgroundJobExecutionContext context)
    {
        if (context.PayloadVersion != PassportExportJob.PayloadVersion)
        {
            return null;
        }

        PassportExportJobPayload? payload;
        try
        {
            payload = context.Payload.Deserialize<PassportExportJobPayload>();
        }
        catch (JsonException)
        {
            return null;
        }

        return payload is not null
            && Guid.TryParseExact(payload.ExportId, "N", out Guid _)
            && !string.IsNullOrWhiteSpace(payload.UserId)
            && Enum.IsDefined(payload.Format)
                ? payload
                : null;
    }
}
