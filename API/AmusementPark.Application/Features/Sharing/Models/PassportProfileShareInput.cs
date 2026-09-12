using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Models;

public sealed record PassportProfileShareInput(
    IReadOnlyCollection<int>? SelectedYears,
    IReadOnlyCollection<string>? SelectedParkIds,
    IReadOnlyCollection<string>? SelectedRatingKeys,
    string? PublicCaption,
    ShareVisibility Visibility,
    bool AllowsComparisons);
