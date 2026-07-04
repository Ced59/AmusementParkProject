using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.Parks.Queries;

public sealed record GetParkDataCompletenessScoreQuery(string ParkId, bool IncludeHidden = true) : IQuery<ApplicationResult<DataCompletenessScore>>;
