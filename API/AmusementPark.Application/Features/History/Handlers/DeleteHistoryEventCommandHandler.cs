using System.Globalization;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.History.Commands;
using AmusementPark.Application.Features.History.Contracts;
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
    private readonly ISeoSitemapRefreshScheduler sitemapRefreshScheduler;

    public DeleteHistoryEventCommandHandler(
        IHistoryEventRepository historyEventRepository,
        ISeoSitemapRefreshScheduler sitemapRefreshScheduler)
    {
        this.historyEventRepository = historyEventRepository;
        this.sitemapRefreshScheduler = sitemapRefreshScheduler;
    }

    public async Task<ApplicationResult> HandleAsync(DeleteHistoryEventCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.EventId))
        {
            return ApplicationResult.Failure(ApplicationErrors.Required("eventId"));
        }

        bool deleted = await this.historyEventRepository.DeleteAsync(command.EventId.Trim(), cancellationToken);
        if (!deleted)
        {
            return ApplicationResult.Failure(ApplicationErrors.EntityNotFound(nameof(HistoryEvent), command.EventId));
        }

        await this.sitemapRefreshScheduler.RequestRefreshAsync(cancellationToken);
        return ApplicationResult.Success();
    }
}
