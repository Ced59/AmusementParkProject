using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AmusementPark.WebAPI.ClientIp;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace AmusementPark.WebAPI.Controllers;

/// <summary>
/// Receives browser-side security reports without exposing implementation details to visitors.
/// </summary>
[ApiController]
[Route("security")]
public sealed class SecurityReportsController : ControllerBase
{
    private const int MaximumReportSizeInBytes = 16_384;
    private readonly ILogger<SecurityReportsController> logger;

    public SecurityReportsController(ILogger<SecurityReportsController> logger)
    {
        this.logger = logger;
    }

    [HttpPost("csp-report")]
    [AllowAnonymous]
    [RequestSizeLimit(MaximumReportSizeInBytes)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ReceiveContentSecurityPolicyReportAsync(CancellationToken cancellationToken = default)
    {
        using StreamReader reader = new StreamReader(
            this.Request.Body,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false,
            bufferSize: 1024,
            leaveOpen: false);

        string reportBody = await reader.ReadToEndAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(reportBody))
        {
            return this.NoContent();
        }

        ContentSecurityPolicyReportSummary summary = ContentSecurityPolicyReportSummary.FromJson(reportBody);

        this.logger.LogWarning(
            "CSP report received. DocumentUri={DocumentUri} ViolatedDirective={ViolatedDirective} EffectiveDirective={EffectiveDirective} BlockedUri={BlockedUri} SourceFile={SourceFile} LineNumber={LineNumber} RemoteIp={RemoteIpAddress} UserAgent={UserAgent}",
            summary.DocumentUri,
            summary.ViolatedDirective,
            summary.EffectiveDirective,
            summary.BlockedUri,
            summary.SourceFile,
            summary.LineNumber,
            ClientIpAddressResolver.Resolve(this.HttpContext),
            this.Request.Headers["User-Agent"].ToString());

        return this.NoContent();
    }


}
