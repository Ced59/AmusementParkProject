using System.Collections.Generic;
using Common.General.Localization;
using Dtos.Parks;
using System.ComponentModel.DataAnnotations;

namespace Dtos.Parks.Updating
{
    public sealed class ParkUpdateDto
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = default!;

        [MaxLength(10)]
        public string? CountryCode { get; set; }

        public ParkTypeDto? Type { get; set; }

        public string? FounderId { get; set; }

        public string? OperatorId { get; set; }

        [Required]
        public double Latitude { get; set; }

        [Required]
        public double Longitude { get; set; }

        public List<LocalizedItem<string>> Descriptions { get; set; } = new();

        public bool IsVisible { get; set; }

        public string? WebsiteUrl { get; set; }

        public string? Street { get; set; }

        public string? City { get; set; }

        public string? PostalCode { get; set; }
    }
}