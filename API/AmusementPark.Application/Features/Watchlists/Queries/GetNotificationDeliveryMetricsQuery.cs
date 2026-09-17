using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Features.Watchlists.Results;

namespace AmusementPark.Application.Features.Watchlists.Queries;

public sealed record GetNotificationDeliveryMetricsQuery(DateTime FromUtc, DateTime ToUtc)
    : IQuery<NotificationDeliveryMetricsResult>;
