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

        [Required]
        public double Latitude { get; set; }

        [Required]
        public double Longitude { get; set; }

        public bool IsVisible { get; set; }

        public string? WebsiteUrl { get; set; }

        public string? Street { get; set; }

        public string? City { get; set; }

        public string? PostalCode { get; set; }
    }
}