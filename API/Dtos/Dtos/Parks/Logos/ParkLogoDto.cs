using System;

namespace Dtos.Parks.Logos
{
    public sealed class ParkLogoDto
    {
        public string Id { get; set; } = default!;
        public string ParkId { get; set; } = default!;
        public string ImageId { get; set; } = default!;
        public string? Description { get; set; }
        public bool IsCurrent { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
