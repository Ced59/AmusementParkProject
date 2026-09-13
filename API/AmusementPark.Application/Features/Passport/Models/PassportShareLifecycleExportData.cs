using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Passport.Models;

public sealed record PassportShareLifecycleExportData(
    IReadOnlyCollection<SharePublication> Publications,
    IReadOnlyCollection<ProfileComparisonInvitation> Invitations,
    IReadOnlyCollection<ProfileComparison> Comparisons,
    IReadOnlyCollection<VisitRecapShareSnapshot> VisitSnapshots,
    IReadOnlyCollection<YearRecapShareSnapshot> YearSnapshots,
    IReadOnlyCollection<PassportProfileShareSnapshot> PassportSnapshots)
{
    public static PassportShareLifecycleExportData Empty { get; } = new(
        Array.Empty<SharePublication>(),
        Array.Empty<ProfileComparisonInvitation>(),
        Array.Empty<ProfileComparison>(),
        Array.Empty<VisitRecapShareSnapshot>(),
        Array.Empty<YearRecapShareSnapshot>(),
        Array.Empty<PassportProfileShareSnapshot>());
}
