using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Services;
using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Core.Domain.Sharing;
using AmusementPark.Core.Domain.Visits;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Sharing.Services;

public sealed class PassportProfileShareSourceFingerprintTests
{
    [Fact]
    public void Create_ShouldIgnoreUnselectedVisitsAndDetectSelectedContentChanges()
    {
        PassportProfileShareInput input = new PassportProfileShareInput(
            new[] { 2026 },
            new[] { "park-1" },
            Array.Empty<string>(),
            null,
            ShareVisibility.Unlisted,
            false);
        PassportProfileSourceData baseline = CreateSource(
            RatingValue.FromDouble(4),
            RatingValue.FromDouble(2));
        PassportProfileSourceData unselectedVisitChanged = CreateSource(
            RatingValue.FromDouble(4),
            RatingValue.FromDouble(5));
        PassportProfileSourceData selectedVisitChanged = CreateSource(
            RatingValue.FromDouble(4.5),
            RatingValue.FromDouble(2));

        string baselineFingerprint = PassportProfileShareSourceFingerprint.Create(baseline, input);

        Assert.Equal(
            baselineFingerprint,
            PassportProfileShareSourceFingerprint.Create(unselectedVisitChanged, input));
        Assert.NotEqual(
            baselineFingerprint,
            PassportProfileShareSourceFingerprint.Create(selectedVisitChanged, input));
    }

    private static PassportProfileSourceData CreateSource(
        RatingValue selectedRating,
        RatingValue unselectedRating)
    {
        return new PassportProfileSourceData(
            new[]
            {
                new PassportVisitStatisticsObservation(
                    "visit-selected",
                    "park-1",
                    VisitDate.ForDay(2026, 6, 14),
                    selectedRating),
                new PassportVisitStatisticsObservation(
                    "visit-unselected",
                    "park-2",
                    VisitDate.ForDay(2025, 7, 1),
                    unselectedRating),
            },
            Array.Empty<PassportRideStatisticsObservation>(),
            "legacy-fingerprint",
            true);
    }
}
