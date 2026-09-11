namespace AmusementPark.Application.Features.Sharing.Results;

public sealed record YearRecapShareSelectionResult(
    string? SavedPublicCaption,
    bool HasSavedSnapshot);
