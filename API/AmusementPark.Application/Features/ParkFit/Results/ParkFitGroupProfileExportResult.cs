namespace AmusementPark.Application.Features.ParkFit.Results;

public sealed record ParkFitGroupProfileExportResult(
    DateTime ExportedAtUtc,
    IReadOnlyCollection<ParkFitGroupProfileExportItemResult> Profiles);
