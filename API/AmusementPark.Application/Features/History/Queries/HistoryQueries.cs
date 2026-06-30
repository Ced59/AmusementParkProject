using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.History.Results;
using AmusementPark.Core.Domain.History;

namespace AmusementPark.Application.Features.History.Queries;

public sealed record GetParkHistoryTimelineQuery(
    string ParkId,
    bool IncludeHidden,
    bool IncludeParkItemEvents,
    IReadOnlyCollection<string> ParkItemIds) : IQuery<ApplicationResult<HistoryTimelineResult>>;

public sealed record GetParkItemHistoryTimelineQuery(
    string ParkItemId,
    bool IncludeHidden) : IQuery<ApplicationResult<HistoryTimelineResult>>;

public sealed record GetHistoryArticleQuery(
    string EventId,
    bool IncludeHidden) : IQuery<ApplicationResult<HistoryArticleResult>>;

public sealed record GetHistoryEventsPageQuery(
    PagedQuery Paging,
    HistoryEntityType? EntityType,
    string? OwnerId,
    string? Search) : IQuery<ApplicationResult<PagedResult<HistoryTimelineEventResult>>>;
