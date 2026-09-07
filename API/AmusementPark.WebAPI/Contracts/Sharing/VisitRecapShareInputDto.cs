namespace AmusementPark.WebAPI.Contracts.Sharing;

public sealed class VisitRecapShareInputDto
{
    public List<string>? SelectedParkItemIds { get; set; }

    public string? PublicCaption { get; set; }
}
