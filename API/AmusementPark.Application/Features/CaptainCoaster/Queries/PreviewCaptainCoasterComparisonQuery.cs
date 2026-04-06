using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.CaptainCoaster.Contracts;
using AmusementPark.Application.Features.CaptainCoaster.Results;

namespace AmusementPark.Application.Features.CaptainCoaster.Queries;

/// <summary>
/// Prévisualise les changements Captain Coaster avant application.
/// </summary>
public sealed record PreviewCaptainCoasterComparisonQuery(CaptainCoasterSourceDescriptor Source) : IQuery<ApplicationResult<CaptainCoasterComparisonPreviewResult>>;
