using System.Globalization;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.History.Commands;
using AmusementPark.Application.Features.History.Contracts;
using AmusementPark.Application.Features.History.Models;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Application.Features.StandaloneAttractions.Ports;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.History.Handlers;

public sealed class DeleteHistoryEventCommandHandler : ICommandHandler<DeleteHistoryEventCommand, ApplicationResult>
{
    private readonly IHistoryEventRepository historyEventRepository;
    private readonly IHistoricalFactRepository historicalFactRepository;
    private readonly ISeoSitemapRefreshScheduler sitemapRefreshScheduler;

    public DeleteHistoryEventCommandHandler(
        IHistoryEventRepository historyEventRepository,
        IHistoricalFactRepository historicalFactRepository,
        ISeoSitemapRefreshScheduler sitemapRefreshScheduler)
    {
        this.historyEventRepository = historyEventRepository
            ?? throw new ArgumentNullException(nameof(historyEventRepository));
        this.historicalFactRepository = historicalFactRepository
            ?? throw new ArgumentNullException(nameof(historicalFactRepository));
        this.sitemapRefreshScheduler = sitemapRefreshScheduler
            ?? throw new ArgumentNullException(nameof(sitemapRefreshScheduler));
    }

    public async Task<ApplicationResult> HandleAsync(DeleteHistoryEventCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.EventId))
        {
            return ApplicationResult.Failure(ApplicationErrors.Required("eventId"));
        }

        string eventId = command.EventId.Trim();
        HistoryEvent? historyEvent = await this.historyEventRepository.GetByIdAsync(
            eventId,
            true,
            cancellationToken);
        if (historyEvent is null)
        {
            return ApplicationResult.Failure(ApplicationErrors.EntityNotFound(nameof(HistoryEvent), command.EventId));
        }

        if (historyEvent.CanonicalFactId.HasValue)
        {
            await this.RetractCanonicalFactAsync(historyEvent.CanonicalFactId.Value, cancellationToken);
        }

        bool deleted = await this.historyEventRepository.DeleteAsync(eventId, cancellationToken);
        if (!deleted)
        {
            return ApplicationResult.Failure(ApplicationErrors.EntityNotFound(nameof(HistoryEvent), command.EventId));
        }

        await this.sitemapRefreshScheduler.RequestRefreshAsync(cancellationToken);
        return ApplicationResult.Success();
    }

    private async Task RetractCanonicalFactAsync(Guid factId, CancellationToken cancellationToken)
    {
        for (int attempt = 0; attempt < 2; attempt++)
        {
            HistoricalFact? latest = await this.historicalFactRepository.GetLatestRevisionAsync(
                factId,
                cancellationToken);
            if (latest is null)
            {
                throw new InvalidOperationException(
                    "A canonical historical fact referenced by a narrative is missing.");
            }

            if (latest.PublicationState == HistoricalPublicationState.Withdrawn)
            {
                return;
            }

            DateTime nowUtc = DateTime.UtcNow;
            DateTime recordedAtUtc = nowUtc > latest.RecordedAtUtc
                ? nowUtc
                : latest.RecordedAtUtc;
            HistoricalFact retraction = latest.CreateRetraction(recordedAtUtc);
            HistoricalReviewEvent reviewEvent = new HistoricalReviewEvent(
                Guid.NewGuid(),
                HistoricalReviewResourceType.Fact,
                factId,
                retraction.Revision,
                HistoricalReviewEventType.Retracted,
                "system:history-narrative-delete",
                "Retrait du fait canonique avant suppression de son récit administratif.",
                recordedAtUtc);
            HistoricalRevisionWriteDisposition outcome =
                await this.historicalFactRepository.AppendRevisionAsync(
                    retraction,
                    reviewEvent,
                    cancellationToken);
            if (outcome is HistoricalRevisionWriteDisposition.Created
                or HistoricalRevisionWriteDisposition.AlreadyExists)
            {
                return;
            }
        }

        throw new InvalidOperationException(
            "The canonical historical fact changed concurrently and could not be retracted safely.");
    }
}
