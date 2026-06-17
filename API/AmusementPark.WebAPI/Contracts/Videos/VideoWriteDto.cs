using AmusementPark.WebAPI.Contracts.Common;

namespace AmusementPark.WebAPI.Contracts.Videos;

public sealed class VideoWriteDto
{
    public string OriginalUrl { get; set; } = string.Empty;

    public VideoOwnerTypeDto OwnerType { get; set; } = VideoOwnerTypeDto.NONE;

    public string OwnerId { get; set; } = string.Empty;

    public VideoTypeDto Type { get; set; } = VideoTypeDto.OTHER;

    public string? Title { get; set; }

    public string? Description { get; set; }

    public string? CreatorName { get; set; }

    public string? CreatorUrl { get; set; }

    public string? ThumbnailUrl { get; set; }

    public long? DurationSeconds { get; set; }

    public DateTime? PublishedAtUtc { get; set; }

    public List<LocalizedTextDto> Titles { get; set; } = new();

    public List<LocalizedTextDto> Descriptions { get; set; } = new();

    public List<string> TagIds { get; set; } = new();

    public bool IsPublished { get; set; } = true;
}
