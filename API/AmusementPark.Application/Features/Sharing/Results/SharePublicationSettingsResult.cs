using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Results;

public sealed record SharePublicationSettingsResult(
    bool IsPublic,
    string? ShareId,
    DateTime? PublishedAtUtc,
    int? PolicySchemaVersion,
    ShareDatePrecision? DatePrecision,
    IReadOnlyCollection<ShareContentField> IncludedFields);
