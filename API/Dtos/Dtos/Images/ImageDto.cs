using System;
using Dtos.Images.Creating;

namespace Dtos.Images
{
    public sealed class ImageDto
    {
        public required string Id { get; set; }

        public required ImageCategoryDto Category { get; set; }

        public required ImageOwnerTypeDto OwnerType { get; set; }

        public string? OwnerId { get; set; }

        public string? Path { get; set; }

        public string? Description { get; set; }

        public bool IsCurrent { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}