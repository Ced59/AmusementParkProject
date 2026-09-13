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

internal sealed record ContentSecurityPolicyReportSummary(
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
