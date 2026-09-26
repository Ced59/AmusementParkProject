using System.Globalization;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.History.Commands;
using AmusementPark.Application.Features.History.Contracts;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.History.Services;
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
    private readonly HistoricalNarrativeCanonicalFactRetractionService canonicalFactRetractionService;
    private readonly ISeoSitemapRefreshScheduler sitemapRefreshScheduler;

    public DeleteHistoryEventCommandHandler(
        IHistoryEventRepository historyEventRepository,
        HistoricalNarrativeCanonicalFactRetractionService canonicalFactRetractionService,
        ISeoSitemapRefreshScheduler sitemapRefreshScheduler)
    {
        this.historyEventRepository = historyEventRepository
            ?? throw new ArgumentNullException(nameof(historyEventRepository));
        this.canonicalFactRetractionService = canonicalFactRetractionService
            ?? throw new ArgumentNullException(nameof(canonicalFactRetractionService));
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
            await this.canonicalFactRetractionService.RetractAsync(
                historyEvent.CanonicalFactId.Value,
                cancellationToken);
        }

        bool deleted = await this.historyEventRepository.DeleteAsync(eventId, cancellationToken);
        if (!deleted)
        {
            return ApplicationResult.Failure(ApplicationErrors.EntityNotFound(nameof(HistoryEvent), command.EventId));
        }

        await this.sitemapRefreshScheduler.RequestRefreshAsync(cancellationToken);
        return ApplicationResult.Success();
    }
}
