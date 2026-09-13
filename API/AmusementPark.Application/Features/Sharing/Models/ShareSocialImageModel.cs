using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Models;

public sealed record ShareSocialImageModel(
    string ShareId,
    SharePublicationType PublicationType,
    string Language,
    string? Subject,
    ShareSocialImageDate? Date,
    int? Year,
    IReadOnlyCollection<ShareSocialImageMetric> Metrics,
    string? Highlight,
    long PublicationVersion);
