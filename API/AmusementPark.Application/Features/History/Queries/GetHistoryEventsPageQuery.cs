using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.History;
using AmusementPark.Application.Features.History.Results;
using AmusementPark.Core.Domain.History;

namespace AmusementPark.Application.Features.History.Queries;

public sealed record GetHistoryEventsPageQuery(
    PagedQuery Paging,
    HistoryEntityType? EntityType,
    string? OwnerId,
    string? Search) : IQuery<ApplicationResult<PagedResult<HistoryTimelineEventResult>>>;
