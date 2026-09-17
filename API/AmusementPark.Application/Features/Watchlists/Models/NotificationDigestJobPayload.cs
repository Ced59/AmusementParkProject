using AmusementPark.Core.Domain.Watchlists;

namespace AmusementPark.Application.Features.Watchlists.Models;

public sealed record NotificationDigestJobPayload(
    string UserId,
    NotificationChannel Channel,
    NotificationFrequency Frequency,
    DateTime PeriodStartUtc);
