using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Queries;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Core.Domain.Sharing;
using AmusementPark.WebAPI.RateLimiting;
using AmusementPark.WebAPI.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Primitives;

namespace AmusementPark.WebAPI.Controllers;

[ApiController]
[Route("sharing/social-images")]
public sealed class SharedPublicationSocialImagesController : ControllerBase
{
    private readonly IQueryHandler<GetSharedPublicationSocialImageQuery, ApplicationResult<ShareSocialImageRenderResult>> handler;

    public SharedPublicationSocialImagesController(
        IQueryHandler<GetSharedPublicationSocialImageQuery, ApplicationResult<ShareSocialImageRenderResult>> handler)
    {
        this.handler = handler ?? throw new ArgumentNullException(nameof(handler));
    }

    [HttpGet("{publicationType}/{shareId}/v{publicationVersion:long}/t{templateVersion:int}/{language}.png")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicyNames.ShareSocialImageRendering)]
    [Produces("image/png")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAsync(
        [FromRoute] string publicationType,
        [FromRoute] string shareId,
        [FromRoute] long publicationVersion,
        [FromRoute] int templateVersion,
        [FromRoute] string language,
        CancellationToken cancellationToken = default)
    {
        if (!TryParsePublicationType(publicationType, out SharePublicationType parsedType)
            || publicationVersion <= 0
            || templateVersion != ShareSocialImageTemplate.Version)
        {
            return this.NotFound();
        }

        ApplicationResult<ShareSocialImageRenderResult> result = await this.handler.HandleAsync(
            new GetSharedPublicationSocialImageQuery(
                shareId,
                parsedType,
                publicationVersion,
                language),
            cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        this.Response.Headers.CacheControl = "public,max-age=300,must-revalidate";
        this.Response.Headers.ETag = result.Value.EntityTag;
        this.Response.Headers.ContentLanguage = language.Trim().ToLowerInvariant();
        this.Response.Headers["Referrer-Policy"] = "no-referrer";
        this.Response.Headers["X-Content-Type-Options"] = "nosniff";
        if (MatchesEntityTag(this.Request.Headers.IfNoneMatch, result.Value.EntityTag))
        {
            return this.StatusCode(StatusCodes.Status304NotModified);
        }

        return this.File(result.Value.Content, result.Value.ContentType);
    }

    internal static bool TryParsePublicationType(
        string? value,
        out SharePublicationType publicationType)
    {
        publicationType = value?.Trim().ToLowerInvariant() switch
        {
            "visit" => SharePublicationType.VisitRecap,
            "year" => SharePublicationType.YearRecap,
            "passport" => SharePublicationType.PassportProfile,
            _ => default,
        };
        return publicationType is SharePublicationType.VisitRecap
            or SharePublicationType.YearRecap
            or SharePublicationType.PassportProfile;
    }

    private static bool MatchesEntityTag(StringValues candidates, string expected)
    {
        return candidates
            .SelectMany(static value => value?.Split(',', StringSplitOptions.TrimEntries)
                ?? Array.Empty<string>())
            .Any(value => MatchesEntityTag(value, expected));
    }

    private static bool MatchesEntityTag(string candidate, string expected)
    {
        if (string.Equals(candidate, "*", StringComparison.Ordinal))
        {
            return true;
        }

        string normalized = candidate.StartsWith("W/", StringComparison.Ordinal)
            ? candidate[2..].TrimStart()
            : candidate;
        return string.Equals(normalized, expected, StringComparison.Ordinal);
    }
}
