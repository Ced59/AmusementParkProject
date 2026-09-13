using AmusementPark.Application.Features.Sharing.Models;

namespace AmusementPark.Infrastructure.Services.Sharing;

internal sealed record ShareSocialImageLocalizedCopy(
    string Culture,
    string VisitTitle,
    string YearTitle,
    string PassportTitle,
    string PersonalLabel,
    string AnonymousSubject,
    string UnknownPark,
    string HighlightLabel,
    IReadOnlyDictionary<ShareSocialImageMetricKind, string> MetricLabels);
