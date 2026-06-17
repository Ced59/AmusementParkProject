using AmusementPark.Core.Domain.Videos;

namespace AmusementPark.Application.Features.Videos.Contracts;

public sealed record VideoSearchCriteria(
    string? Search = null,
    VideoHostingProvider? HostingProvider = null,
    VideoOwnerType? OwnerType = null,
    string? OwnerId = null,
    VideoType? Type = null,
    string? TagId = null,
    string? CreatorName = null,
    bool? IsPublished = null,
    string? SortBy = null,
    string? SortDirection = null);
