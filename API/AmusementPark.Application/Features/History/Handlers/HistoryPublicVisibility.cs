using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.History;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.History.Queries;
using AmusementPark.Application.Features.History.Results;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.History.Handlers;

internal static class HistoryPublicVisibility
{
    public static bool CanExposeTimelineEvent(HistoryTimelineEventResult entry, Park? fallbackContextPark)
    {
        if (entry.Event.EntityType == HistoryEntityType.Park)
        {
            Park? park = entry.ContextPark ?? fallbackContextPark;
            return IsPublicPark(park);
        }

        if (entry.Event.EntityType == HistoryEntityType.ParkItem)
        {
            Park? contextPark = entry.ContextPark ?? fallbackContextPark;
            return IsPublicPark(contextPark) && IsPublicParkItem(entry.ParkItem);
        }

        return false;
    }

    public static bool IsPublicPark(Park? park)
    {
        return park is not null &&
               park.IsVisible &&
               park.AdminReviewStatus != AdminReviewStatus.NotRelevant;
    }

    public static bool IsPublicParkItem(ParkItem? parkItem)
    {
        return parkItem is not null &&
               parkItem.IsVisible &&
               parkItem.AdminReviewStatus != AdminReviewStatus.NotRelevant;
    }
}
