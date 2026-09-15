using AmusementPark.Core.Domain.FactualEvents;

namespace AmusementPark.Application.Features.Watchlists.Results;

public sealed record UserNotificationSourceResult(
    SourceReferenceType Type,
    string PublisherName,
    string Title,
    string Url,
    DateTime PublishedAtUtc,
    DateTime VerifiedAtUtc);
