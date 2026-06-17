using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Videos.Commands;
using AmusementPark.Application.Features.Videos.Contracts;
using AmusementPark.Application.Features.Videos.Queries;
using AmusementPark.Core.Domain.Videos;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Contracts.Common;
using AmusementPark.WebAPI.Contracts.Videos;
using AmusementPark.WebAPI.Filters;
using AmusementPark.WebAPI.Mappers;
using AmusementPark.WebAPI.OutputCaching;
using AmusementPark.WebAPI.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace AmusementPark.WebAPI.Controllers;

[ApiController]
[Route("videos")]
public sealed class VideosController : ControllerBase
{
    private readonly ICommandHandler<CreateVideoCommand, ApplicationResult<Video>> createVideoCommandHandler;
    private readonly ICommandHandler<UpdateVideoCommand, ApplicationResult<Video>> updateVideoCommandHandler;
    private readonly ICommandHandler<DeleteVideoCommand, ApplicationResult> deleteVideoCommandHandler;
    private readonly ICommandHandler<CreateVideoTagCommand, ApplicationResult<VideoTag>> createVideoTagCommandHandler;
    private readonly ICommandHandler<UpdateVideoTagCommand, ApplicationResult<VideoTag>> updateVideoTagCommandHandler;
    private readonly IQueryHandler<GetVideoByIdQuery, ApplicationResult<Video>> getVideoByIdQueryHandler;
    private readonly IQueryHandler<GetVideosPageQuery, ApplicationResult<PagedResult<Video>>> getVideosPageQueryHandler;
    private readonly IQueryHandler<ListVideoTagsQuery, ApplicationResult<IReadOnlyCollection<VideoTag>>> listVideoTagsQueryHandler;
    private readonly IQueryHandler<ResolveVideoMetadataQuery, ApplicationResult<ResolvedVideoMetadata>> resolveVideoMetadataQueryHandler;

    public VideosController(
        ICommandHandler<CreateVideoCommand, ApplicationResult<Video>> createVideoCommandHandler,
        ICommandHandler<UpdateVideoCommand, ApplicationResult<Video>> updateVideoCommandHandler,
        ICommandHandler<DeleteVideoCommand, ApplicationResult> deleteVideoCommandHandler,
        ICommandHandler<CreateVideoTagCommand, ApplicationResult<VideoTag>> createVideoTagCommandHandler,
        ICommandHandler<UpdateVideoTagCommand, ApplicationResult<VideoTag>> updateVideoTagCommandHandler,
        IQueryHandler<GetVideoByIdQuery, ApplicationResult<Video>> getVideoByIdQueryHandler,
        IQueryHandler<GetVideosPageQuery, ApplicationResult<PagedResult<Video>>> getVideosPageQueryHandler,
        IQueryHandler<ListVideoTagsQuery, ApplicationResult<IReadOnlyCollection<VideoTag>>> listVideoTagsQueryHandler,
        IQueryHandler<ResolveVideoMetadataQuery, ApplicationResult<ResolvedVideoMetadata>> resolveVideoMetadataQueryHandler)
    {
        this.createVideoCommandHandler = createVideoCommandHandler;
        this.updateVideoCommandHandler = updateVideoCommandHandler;
        this.deleteVideoCommandHandler = deleteVideoCommandHandler;
        this.createVideoTagCommandHandler = createVideoTagCommandHandler;
        this.updateVideoTagCommandHandler = updateVideoTagCommandHandler;
        this.getVideoByIdQueryHandler = getVideoByIdQueryHandler;
        this.getVideosPageQueryHandler = getVideosPageQueryHandler;
        this.listVideoTagsQueryHandler = listVideoTagsQueryHandler;
        this.resolveVideoMetadataQueryHandler = resolveVideoMetadataQueryHandler;
    }

    [HttpGet]
    [AllowAnonymous]
    [OutputCache(PolicyName = ApiOutputCachePolicyNames.PublicDataMedium)]
    [ProducesResponseType(typeof(PagedResponseDto<VideoDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetVideosAsync(
        [FromQuery] PaginationRequestDto pagination,
        [FromQuery] string? search = null,
        [FromQuery] VideoHostingProviderDto? hostingProvider = null,
        [FromQuery] VideoOwnerTypeDto? ownerType = null,
        [FromQuery] string? ownerId = null,
        [FromQuery] VideoTypeDto? type = null,
        [FromQuery] string? tagId = null,
        [FromQuery] string? creatorName = null,
        [FromQuery] string? languageCode = null,
        [FromQuery] bool? isPublished = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDirection = null,
        CancellationToken cancellationToken = default)
    {
        bool canSeeNonVisible = this.UserCanSeeNonVisible();
        VideoSearchCriteria criteria = new VideoSearchCriteria(
            search,
            hostingProvider.ToOptionalDomain(),
            ownerType.ToOptionalDomain(),
            ownerId,
            type.ToOptionalDomain(),
            tagId,
            creatorName,
            languageCode,
            canSeeNonVisible ? isPublished : true,
            sortBy,
            sortDirection);

        ApplicationResult<PagedResult<Video>> result = await this.getVideosPageQueryHandler.HandleAsync(new GetVideosPageQuery(pagination.ToApplication(), criteria), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        PagedResponseDto<VideoDto> response = result.Value.ToPagedResponse(static video => video.ToHttp());
        return this.Ok(response);
    }

    [HttpGet("{videoId}")]
    [AllowAnonymous]
    [OutputCache(PolicyName = ApiOutputCachePolicyNames.PublicDataMedium)]
    [ProducesResponseType(typeof(VideoDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetVideoAsync([FromRoute] string videoId, [FromQuery] string? languageCode = null, CancellationToken cancellationToken = default)
    {
        ApplicationResult<Video> result = await this.getVideoByIdQueryHandler.HandleAsync(new GetVideoByIdQuery(videoId), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        if (!this.UserCanSeeNonVisible() && !result.Value.IsPublished)
        {
            return this.ToNotFoundProblemDetailsResult("Video does not exist.", "video.not-found");
        }

        if (!this.UserCanSeeNonVisible() && !IsVisibleForLanguage(result.Value, languageCode))
        {
            return this.ToNotFoundProblemDetailsResult("Video does not exist.", "video.not-found");
        }

        return this.Ok(result.Value.ToHttp());
    }

    [HttpGet("resolve-metadata")]
    [RequireActivatedUnblockedUser]
    [Authorize(Roles = AuthorizationRoleGroups.Admin)]
    [ProducesResponseType(typeof(ResolvedVideoMetadataDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ResolveMetadataAsync([FromQuery] string url, CancellationToken cancellationToken = default)
    {
        ApplicationResult<ResolvedVideoMetadata> result = await this.resolveVideoMetadataQueryHandler.HandleAsync(new ResolveVideoMetadataQuery(url), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }

    [HttpPost]
    [RequireActivatedUnblockedUser]
    [Authorize(Roles = AuthorizationRoleGroups.Admin)]
    [InvalidatesPublicCache(PublicCacheScope.Data)]
    [AdminAudit("video.create", "Video")]
    [ProducesResponseType(typeof(VideoDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateVideoAsync([FromBody] VideoWriteDto request, CancellationToken cancellationToken = default)
    {
        ApplicationResult<Video> result = await this.createVideoCommandHandler.HandleAsync(new CreateVideoCommand(request.ToApplication()), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }

    [HttpPut("{videoId}")]
    [RequireActivatedUnblockedUser]
    [Authorize(Roles = AuthorizationRoleGroups.Admin)]
    [InvalidatesPublicCache(PublicCacheScope.Data)]
    [AdminAudit("video.update", "Video", TargetIdRouteKey = "videoId")]
    [ProducesResponseType(typeof(VideoDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateVideoAsync([FromRoute] string videoId, [FromBody] VideoWriteDto request, CancellationToken cancellationToken = default)
    {
        ApplicationResult<Video> result = await this.updateVideoCommandHandler.HandleAsync(new UpdateVideoCommand(videoId, request.ToApplication()), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }

    [HttpDelete("{videoId}")]
    [RequireActivatedUnblockedUser]
    [Authorize(Roles = AuthorizationRoleGroups.Admin)]
    [InvalidatesPublicCache(PublicCacheScope.Data)]
    [AdminAudit("video.delete", "Video", TargetIdRouteKey = "videoId")]
    public async Task<IActionResult> DeleteVideoAsync([FromRoute] string videoId, CancellationToken cancellationToken = default)
    {
        ApplicationResult result = await this.deleteVideoCommandHandler.HandleAsync(new DeleteVideoCommand(videoId), cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(true);
    }

    [HttpGet("tags")]
    [AllowAnonymous]
    [OutputCache(PolicyName = ApiOutputCachePolicyNames.PublicReferenceData)]
    [ProducesResponseType(typeof(PagedResponseDto<VideoTagDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTagsAsync([FromQuery] PaginationRequestDto pagination, CancellationToken cancellationToken = default)
    {
        ApplicationResult<IReadOnlyCollection<VideoTag>> result = await this.listVideoTagsQueryHandler.HandleAsync(new ListVideoTagsQuery(), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        PagedResponseDto<VideoTagDto> response = pagination.ToPagedResponse(result.Value, static tag => tag.ToHttp());
        return this.Ok(response);
    }

    [HttpPost("tags")]
    [RequireActivatedUnblockedUser]
    [Authorize(Roles = AuthorizationRoleGroups.Admin)]
    [InvalidatesPublicCache(PublicCacheScope.ReferenceData)]
    [AdminAudit("video-tag.create", "VideoTag")]
    [ProducesResponseType(typeof(VideoTagDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateTagAsync([FromBody] CreateVideoTagRequest request, CancellationToken cancellationToken = default)
    {
        ApplicationResult<VideoTag> result = await this.createVideoTagCommandHandler.HandleAsync(new CreateVideoTagCommand(request.ToApplication()), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }

    [HttpPut("tags/{id}")]
    [RequireActivatedUnblockedUser]
    [Authorize(Roles = AuthorizationRoleGroups.Admin)]
    [InvalidatesPublicCache(PublicCacheScope.ReferenceData)]
    [AdminAudit("video-tag.update", "VideoTag", TargetIdRouteKey = "id")]
    [ProducesResponseType(typeof(VideoTagDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateTagAsync([FromRoute] string id, [FromBody] UpdateVideoTagRequest request, CancellationToken cancellationToken = default)
    {
        ApplicationResult<VideoTag> result = await this.updateVideoTagCommandHandler.HandleAsync(new UpdateVideoTagCommand(id, request.ToApplication()), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }

    private bool UserCanSeeNonVisible()
    {
        return this.User?.IsInRole("ADMIN") == true;
    }

    private static bool IsVisibleForLanguage(Video video, string? languageCode)
    {
        if (string.IsNullOrWhiteSpace(languageCode) || video.LanguageCodes.Count == 0)
        {
            return true;
        }

        string normalizedLanguageCode = NormalizeLanguageCode(languageCode);
        return video.LanguageCodes.Any(videoLanguageCode => string.Equals(NormalizeLanguageCode(videoLanguageCode), normalizedLanguageCode, StringComparison.Ordinal));
    }

    private static string NormalizeLanguageCode(string languageCode)
    {
        string normalizedLanguageCode = languageCode.Trim().ToLowerInvariant();
        return normalizedLanguageCode.Length >= 2 ? normalizedLanguageCode[..2] : normalizedLanguageCode;
    }
}
