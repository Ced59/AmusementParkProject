using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Passport.Models;

public sealed record PassportShareLifecycleExportData(
    IReadOnlyCollection<SharePublication> Publications,
    IReadOnlyCollection<ProfileComparisonInvitation> Invitations,
    IReadOnlyCollection<ProfileComparison> Comparisons)
{
    public static PassportShareLifecycleExportData Empty { get; } = new(
        Array.Empty<SharePublication>(),
        Array.Empty<ProfileComparisonInvitation>(),
        Array.Empty<ProfileComparison>());
}
