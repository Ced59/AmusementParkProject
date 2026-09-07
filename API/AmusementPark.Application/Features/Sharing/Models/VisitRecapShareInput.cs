namespace AmusementPark.Application.Features.Sharing.Models;

public sealed record VisitRecapShareInput(
    IReadOnlyCollection<string>? SelectedParkItemIds,
    string? PublicCaption);
