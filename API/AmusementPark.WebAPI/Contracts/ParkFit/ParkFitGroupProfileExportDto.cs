namespace AmusementPark.WebAPI.Contracts.ParkFit;

public sealed class ParkFitGroupProfileExportDto
{
    public DateTime ExportedAtUtc { get; set; }

    public IReadOnlyCollection<ParkFitGroupProfileExportItemDto> Profiles { get; set; } =
        Array.Empty<ParkFitGroupProfileExportItemDto>();
}
