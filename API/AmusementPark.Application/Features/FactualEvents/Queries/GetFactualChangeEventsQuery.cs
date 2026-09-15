using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.FactualEvents.Models;
using AmusementPark.Application.Features.FactualEvents.Results;

namespace AmusementPark.Application.Features.FactualEvents.Queries;

public sealed record GetFactualChangeEventsQuery(
    FactualChangeEventSearchCriteria Criteria)
    : IQuery<ApplicationResult<PagedResult<FactualChangeEventAdminResult>>>;
