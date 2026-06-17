using AmusementPark.Application.Common.Contracts;
using AmusementPark.Core.Domain.Videos;

namespace AmusementPark.Application.Features.Videos.Contracts;

public sealed class VideoWriteModel
{
    public string OriginalUrl { get; init; } = string.Empty;

    public VideoOwnerType OwnerType { get; init; } = VideoOwnerType.None;

    public string OwnerId { get; init; } = string.Empty;

    public VideoType Type { get; init; } = VideoType.Other;

    public string? Title { get; init; }

    public string? Description { get; init; }

    public string? CreatorName { get; init; }

    public string? CreatorUrl { get; init; }

    public string? ThumbnailUrl { get; init; }

    public TimeSpan? Duration { get; init; }

    public DateTime? PublishedAtUtc { get; init; }

    public IReadOnlyCollection<string> LanguageCodes { get; init; } = Array.Empty<string>();

    public IReadOnlyCollection<LocalizedTextValue> Titles { get; init; } = Array.Empty<LocalizedTextValue>();

    public IReadOnlyCollection<LocalizedTextValue> Descriptions { get; init; } = Array.Empty<LocalizedTextValue>();

    public IReadOnlyCollection<string> TagIds { get; init; } = Array.Empty<string>();

    public bool IsPublished { get; init; } = true;
}
