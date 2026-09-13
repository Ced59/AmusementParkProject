using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Results;

public sealed record SharePublicationSettingsResult(
    bool IsPublic,
    string? ShareId,
    DateTime? PublishedAtUtc,
    int? PolicySchemaVersion,
    ShareDatePrecision? DatePrecision,
    IReadOnlyCollection<ShareContentField> IncludedFields,
    ShareVisibility? Visibility = null,
    string? PublicationId = null,
    long? PublicationVersion = null,
    bool IsModerationSuspended = false);
