using AmusementPark.Core.Domain.FactualEvents;

namespace AmusementPark.Application.Features.FactualEvents.Results;

public sealed record FactualSourceReferenceAdminResult(
    SourceReferenceType Type,
    string PublisherName,
    string Title,
    string Url,
    DateTime PublishedAtUtc);
