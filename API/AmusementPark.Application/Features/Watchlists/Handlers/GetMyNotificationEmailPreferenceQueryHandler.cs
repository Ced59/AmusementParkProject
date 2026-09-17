using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Watchlists.Queries;
using AmusementPark.Application.Features.Watchlists.Results;
using AmusementPark.Application.Features.Watchlists.Services;

namespace AmusementPark.Application.Features.Watchlists.Handlers;

public sealed class GetMyNotificationEmailPreferenceQueryHandler
    : IQueryHandler<GetMyNotificationEmailPreferenceQuery,
        ApplicationResult<NotificationEmailPreferenceResult>>
{
    private readonly NotificationEmailPreferenceService service;

    public GetMyNotificationEmailPreferenceQueryHandler(NotificationEmailPreferenceService service)
    {
        this.service = service;
    }

    public Task<ApplicationResult<NotificationEmailPreferenceResult>> HandleAsync(
        GetMyNotificationEmailPreferenceQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        return this.service.GetAsync(query.UserId, cancellationToken);
    }
}
