using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Features.Watchlists.Results;
using AmusementPark.Core.Domain.Watchlists;

namespace AmusementPark.Application.Features.Watchlists.Queries;

public sealed record PreviewNotificationDigestQuery(
    string UserId,
    NotificationFrequency Frequency,
    DateTime PeriodStartUtc) : IQuery<NotificationDigestPreviewResult?>;
