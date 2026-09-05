using Microsoft.AspNetCore.Http;

namespace AmusementPark.WebAPI.Contracts.Parks;

public sealed class ParkOfficialMapFileCreateDto
{
    public string ParkId { get; set; } = string.Empty;

    public string OfficialMapId { get; set; } = string.Empty;

    public IFormFile? File { get; set; }
}
