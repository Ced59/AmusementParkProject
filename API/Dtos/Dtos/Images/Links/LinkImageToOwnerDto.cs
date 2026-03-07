using Dtos.Images.Creating;

namespace Dtos.Images.Links
{
    public sealed class LinkImageToOwnerDto
    {
        public required string ImageId { get; set; }

        public required ImageOwnerTypeDto OwnerType { get; set; }

        public required string OwnerId { get; set; }

        public string? Description { get; set; }

        public bool SetAsCurrent { get; set; } = true;
    }
}