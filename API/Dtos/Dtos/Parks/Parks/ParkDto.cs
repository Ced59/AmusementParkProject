using System.Collections.Generic;
using Common.General.Localization;
using Dtos.Parks;

namespace Dtos.Parks.Parks
{
    public class ParkDto
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? CountryCode { get; set; }
        public ParkTypeDto? Type { get; set; }
        public string? FounderId { get; set; }
        public string? OperatorId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public List<LocalizedItem<string>> Descriptions { get; set; } = new();
        public bool IsVisible { get; set; } = false;
        public string? WebSiteUrl { get; set; }
        public string? Street { get; set; }
        public string? City { get; set; }
        public string? PostalCode { get; set; }
        public string? CurrentLogoImageId { get; set; }
    }
}