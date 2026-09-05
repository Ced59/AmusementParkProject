using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Parks.Contracts;
using AmusementPark.Application.Features.Parks.Queries;
using AmusementPark.WebAPI.AdminPublicView;
using AmusementPark.WebAPI.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace AmusementPark.WebAPI.Controllers;

[ApiController]
[Route("parks/{parkId}/official-maps")]
public sealed class ParkOfficialMapsController : ControllerBase
{
    private readonly IQueryHandler<GetParkOfficialMapFileQuery, ApplicationResult<ParkOfficialMapBinary>> fileHandler;

    public ParkOfficialMapsController(
        IQueryHandler<GetParkOfficialMapFileQuery, ApplicationResult<ParkOfficialMapBinary>> fileHandler)
    {
        this.fileHandler = fileHandler;
    }

    [HttpGet("{officialMapId}/file")]
    [HttpHead("{officialMapId}/file")]
    [AllowAnonymous]
    public async Task<IActionResult> GetFileAsync(
        [FromRoute] string parkId,
        [FromRoute] string officialMapId,
        CancellationToken cancellationToken = default)
    {
        bool includeHidden = this.HttpContext.UserCanSeeNonVisibleInPublicView();
        ApplicationResult<ParkOfficialMapBinary> result = await this.fileHandler.HandleAsync(
            new GetParkOfficialMapFileQuery(parkId, officialMapId, includeHidden),
            cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        ParkOfficialMapBinary binary = result.Value;
        ContentDispositionHeaderValue disposition = new ContentDispositionHeaderValue(
            binary.DisplayInline ? "inline" : "attachment")
        {
            FileNameStar = binary.FileName,
        };
        this.Response.Headers.ContentDisposition = disposition.ToString();
        this.Response.Headers.CacheControl = includeHidden
            ? "private,no-store"
            : "public,max-age=86400";
        this.Response.Headers[HeaderNames.XContentTypeOptions] = "nosniff";
        this.Response.Headers.AcceptRanges = "bytes";
        this.Response.ContentType = binary.ContentType;
        this.Response.ContentLength = binary.SizeInBytes;

        if (HttpMethods.IsHead(this.Request.Method))
        {
            return new EmptyResult();
        }

        bool hasRange = !string.IsNullOrWhiteSpace(this.Request.Headers.Range);
        if (!TryResolveByteRange(
            this.Request.Headers.Range.ToString(),
            binary.SizeInBytes,
            out long start,
            out long end))
        {
            this.Response.StatusCode = StatusCodes.Status416RangeNotSatisfiable;
            this.Response.Headers.ContentRange = $"bytes */{binary.SizeInBytes}";
            this.Response.ContentLength = 0;
            return new EmptyResult();
        }

        long? length = null;
        if (hasRange)
        {
            length = end - start + 1;
            this.Response.StatusCode = StatusCodes.Status206PartialContent;
            this.Response.Headers.ContentRange = $"bytes {start}-{end}/{binary.SizeInBytes}";
            this.Response.ContentLength = length.Value;
        }

        await binary.CopyToAsync(this.Response.Body, start, length, cancellationToken);
        return new EmptyResult();
    }

    private static bool TryResolveByteRange(string rangeHeader, long size, out long start, out long end)
    {
        start = 0;
        end = size - 1;
        if (string.IsNullOrWhiteSpace(rangeHeader))
        {
            return size > 0;
        }

        const string bytesPrefix = "bytes=";
        if (size <= 0
            || !rangeHeader.StartsWith(bytesPrefix, StringComparison.OrdinalIgnoreCase)
            || rangeHeader.Contains(',', StringComparison.Ordinal))
        {
            return false;
        }

        string value = rangeHeader[bytesPrefix.Length..].Trim();
        int separatorIndex = value.IndexOf('-', StringComparison.Ordinal);
        if (separatorIndex < 0 || value.IndexOf('-', separatorIndex + 1) >= 0)
        {
            return false;
        }

        string startValue = value[..separatorIndex].Trim();
        string endValue = value[(separatorIndex + 1)..].Trim();
        if (startValue.Length == 0)
        {
            if (!long.TryParse(endValue, out long suffixLength) || suffixLength <= 0)
            {
                return false;
            }

            start = Math.Max(0, size - suffixLength);
            return true;
        }

        if (!long.TryParse(startValue, out start) || start < 0 || start >= size)
        {
            return false;
        }

        if (endValue.Length == 0)
        {
            return true;
        }

        if (!long.TryParse(endValue, out end) || end < start)
        {
            return false;
        }

        end = Math.Min(end, size - 1);
        return true;
    }
}
