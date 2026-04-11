using System.Collections.Generic;
using AmusementPark.WebAPI.Contracts.Common;

namespace AmusementPark.WebAPI.Contracts.Parks;

public sealed class ParkCreateDto
{
    public string? Name { get; set; }

    public string? CountryCode { get; set; }

    public ParkTypeDto? Type { get; set; }

    public string? FounderId { get; set; }

    public string? OperatorId { get; set; }

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public List<LocalizedTextDto> Descriptions { get; set; } = new();

    public bool IsVisible { get; set; } = false;

    public string? WebsiteUrl { get; set; }

    public string? Street { get; set; }

    public string? City { get; set; }

    public string? PostalCode { get; set; }
}
