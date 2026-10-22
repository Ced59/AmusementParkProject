using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.StandaloneAttractions.Queries;

public sealed record GetStandaloneAttractionDataCompletenessScoreQuery(
    string StandaloneAttractionId,
    bool IncludeHidden = true,
    bool ProjectForPublication = false) : IQuery<ApplicationResult<DataCompletenessScore>>;
