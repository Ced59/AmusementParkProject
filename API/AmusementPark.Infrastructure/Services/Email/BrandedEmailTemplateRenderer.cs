using System.Net;
using System.Text;

namespace AmusementPark.Infrastructure.Services.Email;

public sealed record BrandedEmailAction(string Label, string Url);

public sealed record BrandedEmailMetric(string Label, string Value);

public sealed record BrandedEmailHighlight(string Label, string Text);

public sealed class BrandedEmailTemplateModel
{
    public string Preheader { get; init; } = string.Empty;

    public string Badge { get; init; } = "Amusement Park";

    public string Title { get; init; } = string.Empty;

    public IReadOnlyCollection<string> Paragraphs { get; init; } = Array.Empty<string>();

    public BrandedEmailAction? Action { get; init; }

    public IReadOnlyCollection<BrandedEmailMetric> Metrics { get; init; } = Array.Empty<BrandedEmailMetric>();

    public BrandedEmailHighlight? Highlight { get; init; }

    public string FooterNote { get; init; } = "Amusement Park";
}

public sealed class BrandedEmailTemplateRenderer
{
    public string Render(BrandedEmailTemplateModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        string preheader = Encode(model.Preheader);
        string badge = Encode(model.Badge);
        string title = Encode(model.Title);
        string paragraphs = this.BuildParagraphs(model.Paragraphs);
        string action = this.BuildAction(model.Action);
        string metrics = this.BuildMetrics(model.Metrics);
        string highlight = this.BuildHighlight(model.Highlight);
        string footerNote = Encode(model.FooterNote);

        return $"""
<!doctype html>
<html lang="en">
  <head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1">
    <title>{title}</title>
  </head>
  <body style="margin:0;padding:0;background:#fff9f0;color:#2a1a08;font-family:'Plus Jakarta Sans','Segoe UI',Arial,sans-serif;">
    <div style="display:none;max-height:0;overflow:hidden;opacity:0;color:transparent;">{preheader}</div>
    <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="background:#fff9f0;padding:28px 12px;">
      <tr>
        <td align="center">
          <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="max-width:640px;background:#ffffff;border:1px solid rgba(143,78,13,0.18);border-radius:22px;overflow:hidden;box-shadow:0 18px 44px rgba(42,26,8,0.12);">
            <tr>
              <td style="background:#160a00;padding:26px 28px;color:#ffffff;">
                <div style="font-size:14px;font-weight:800;letter-spacing:0.08em;text-transform:uppercase;">
                  AMUSEMENT-PARKS<span style="color:#ff5a1f;">.</span><span style="color:#38c5f5;">f</span><span style="color:#ff4d8b;">u</span><span style="color:#c4ff27;">n</span>
                </div>
                <div style="margin-top:16px;display:inline-block;background:#ff5a1f;color:#160a00;border-radius:999px;padding:7px 12px;font-size:12px;font-weight:800;text-transform:uppercase;letter-spacing:0.05em;">{badge}</div>
                <h1 style="margin:18px 0 0;font-size:30px;line-height:1.15;font-weight:800;color:#ffffff;">{title}</h1>
              </td>
            </tr>
            <tr>
              <td style="padding:28px;">
                {paragraphs}
                {metrics}
                {highlight}
                {action}
              </td>
            </tr>
            <tr>
              <td style="background:#fff3e0;border-top:1px solid rgba(143,78,13,0.16);padding:18px 28px;color:#5f411e;font-size:13px;line-height:1.6;">
                {footerNote}
              </td>
            </tr>
          </table>
        </td>
      </tr>
    </table>
  </body>
</html>
""";
    }

    private string BuildParagraphs(IReadOnlyCollection<string> paragraphs)
    {
        if (paragraphs.Count == 0)
        {
            return string.Empty;
        }

        StringBuilder builder = new StringBuilder();
        foreach (string paragraph in paragraphs)
        {
            if (!string.IsNullOrWhiteSpace(paragraph))
            {
                builder.Append("<p style=\"margin:0 0 16px;color:#2a1a08;font-size:16px;line-height:1.7;\">");
                builder.Append(Encode(paragraph));
                builder.AppendLine("</p>");
            }
        }

        return builder.ToString();
    }

    private string BuildAction(BrandedEmailAction? action)
    {
        if (action is null)
        {
            return string.Empty;
        }

        return $"""
<table role="presentation" cellspacing="0" cellpadding="0" style="margin-top:24px;">
  <tr>
    <td style="border-radius:999px;background:#ff5a1f;">
      <a href="{EncodeAttribute(action.Url)}" style="display:inline-block;padding:13px 22px;color:#160a00;font-size:15px;font-weight:800;text-decoration:none;">{Encode(action.Label)}</a>
    </td>
  </tr>
</table>
""";
    }

    private string BuildMetrics(IReadOnlyCollection<BrandedEmailMetric> metrics)
    {
        if (metrics.Count == 0)
        {
            return string.Empty;
        }

        StringBuilder builder = new StringBuilder();
        builder.AppendLine("<table role=\"presentation\" width=\"100%\" cellspacing=\"0\" cellpadding=\"0\" style=\"margin:22px 0;border-collapse:separate;border-spacing:0 10px;\">");

        foreach (BrandedEmailMetric metric in metrics)
        {
            builder.Append("<tr><td style=\"background:#fffaf3;border:1px solid rgba(143,78,13,0.14);border-radius:14px;padding:12px 14px;\">");
            builder.Append("<div style=\"color:#5f411e;font-size:12px;font-weight:800;text-transform:uppercase;letter-spacing:0.05em;\">");
            builder.Append(Encode(metric.Label));
            builder.Append("</div><div style=\"margin-top:4px;color:#160a00;font-size:18px;font-weight:800;\">");
            builder.Append(Encode(metric.Value));
            builder.AppendLine("</div></td></tr>");
        }

        builder.AppendLine("</table>");
        return builder.ToString();
    }

    private string BuildHighlight(BrandedEmailHighlight? highlight)
    {
        if (highlight is null)
        {
            return string.Empty;
        }

        return $"""
<div style="margin-top:22px;background:#fffaf3;border-left:5px solid #38c5f5;border-radius:16px;padding:16px 18px;">
  <div style="color:#006d93;font-size:12px;font-weight:800;text-transform:uppercase;letter-spacing:0.05em;">{Encode(highlight.Label)}</div>
  <div style="margin-top:10px;color:#2a1a08;font-size:15px;line-height:1.7;white-space:pre-line;">{Encode(highlight.Text)}</div>
</div>
""";
    }

    private static string Encode(string? value)
    {
        return WebUtility.HtmlEncode(value ?? string.Empty);
    }

    private static string EncodeAttribute(string? value)
    {
        return Encode(value).Replace("\"", "&quot;", StringComparison.Ordinal);
    }
}
