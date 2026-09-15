using AmusementPark.Application.Common.Requests;
using AmusementPark.Core.Domain.FactualEvents;

namespace AmusementPark.Application.Features.FactualEvents.Models;

public sealed record FactualChangeEventSearchCriteria(
    PagedQuery Paging,
    FactualChangeStatus? Status = null,
    FactualTargetType? TargetType = null,
    FactualEventType? EventType = null,
    DataConfidence? Confidence = null);
