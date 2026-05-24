using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
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
            this.HttpContext.Connection.RemoteIpAddress?.ToString(),
            this.Request.Headers["User-Agent"].ToString());

        return this.NoContent();
    }

    private sealed record ContentSecurityPolicyReportSummary(
        string? DocumentUri,
        string? ViolatedDirective,
        string? EffectiveDirective,
        string? BlockedUri,
        string? SourceFile,
        int? LineNumber)
    {
        public static ContentSecurityPolicyReportSummary FromJson(string reportBody)
        {
            try
            {
                using JsonDocument document = JsonDocument.Parse(reportBody);
                JsonElement reportElement = document.RootElement;

                if (reportElement.TryGetProperty("csp-report", out JsonElement nestedReportElement))
                {
                    reportElement = nestedReportElement;
                }

                return new ContentSecurityPolicyReportSummary(
                    ReadString(reportElement, "document-uri"),
                    ReadString(reportElement, "violated-directive"),
                    ReadString(reportElement, "effective-directive"),
                    ReadString(reportElement, "blocked-uri"),
                    ReadString(reportElement, "source-file"),
                    ReadInt32(reportElement, "line-number"));
            }
            catch (JsonException)
            {
                return new ContentSecurityPolicyReportSummary(
                    DocumentUri: null,
                    ViolatedDirective: "invalid-json-report",
                    EffectiveDirective: null,
                    BlockedUri: null,
                    SourceFile: null,
                    LineNumber: null);
            }
        }

        private static string? ReadString(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out JsonElement property))
            {
                return null;
            }

            return property.ValueKind == JsonValueKind.String
                ? property.GetString()
                : property.ToString();
        }

        private static int? ReadInt32(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out JsonElement property))
            {
                return null;
            }

            if (property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out int value))
            {
                return value;
            }

            return null;
        }
    }
}
