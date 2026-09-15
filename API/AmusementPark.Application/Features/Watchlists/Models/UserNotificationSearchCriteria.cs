using AmusementPark.Application.Common.Requests;
using AmusementPark.Core.Domain.FactualEvents;

namespace AmusementPark.Application.Features.Watchlists.Models;

public sealed record UserNotificationSearchCriteria(
    PagedQuery Paging,
    bool UnreadOnly,
    string? ParkId,
    FactualEventType? EventType);
