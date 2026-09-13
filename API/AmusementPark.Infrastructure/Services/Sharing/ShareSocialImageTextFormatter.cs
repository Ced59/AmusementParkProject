using System.Globalization;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Core.Domain.Sharing;
using SixLabors.Fonts;

namespace AmusementPark.Infrastructure.Services.Sharing;

internal static class ShareSocialImageTextFormatter
{
    public static string BuildAlternativeText(
        ShareSocialImageModel model,
        ShareSocialImageLocalizedCopy copy,
        CultureInfo culture)
    {
        string title = ResolveTitle(model, copy);
        string subject = ResolveSubject(model, copy);
        string context = ResolveContext(model, culture);
        List<string> details = model.Metrics
            .Take(3)
            .Select(metric =>
                $"{FormatMetric(metric, culture)} {ResolveMetricLabel(metric.Kind, copy).ToLower(culture)}")
            .ToList();
        string heading = context.Length == 0
            ? $"{title} — {subject}"
            : $"{title} — {subject}, {context}";
        return details.Count == 0
            ? heading
            : $"{heading}. {string.Join(", ", details)}.";
    }

    public static string ResolveTitle(
        ShareSocialImageModel model,
        ShareSocialImageLocalizedCopy copy)
    {
        return model.PublicationType switch
        {
            SharePublicationType.VisitRecap => copy.VisitTitle,
            SharePublicationType.YearRecap => copy.YearTitle,
            SharePublicationType.PassportProfile => copy.PassportTitle,
            _ => copy.PassportTitle,
        };
    }

    public static string ResolveSubject(
        ShareSocialImageModel model,
        ShareSocialImageLocalizedCopy copy)
    {
        string normalizedSubject = model.Subject?.Trim() ?? string.Empty;
        if (normalizedSubject.Length > 0)
        {
            return normalizedSubject;
        }

        return model.PublicationType == SharePublicationType.VisitRecap
            ? copy.UnknownPark
            : copy.AnonymousSubject;
    }

    public static string ResolveContext(ShareSocialImageModel model, CultureInfo culture)
    {
        if (model.Year.HasValue)
        {
            return model.Year.Value.ToString(CultureInfo.InvariantCulture);
        }

        if (model.Date is null)
        {
            return string.Empty;
        }

        ShareSocialImageDate date = model.Date;
        if (date.Precision == ShareDatePrecision.Year)
        {
            return date.Year.ToString(CultureInfo.InvariantCulture);
        }

        int month = date.Month.GetValueOrDefault(1);
        if (month is < 1 or > 12)
        {
            return date.Year.ToString(CultureInfo.InvariantCulture);
        }

        if (date.Precision == ShareDatePrecision.Month
            || !date.Day.HasValue)
        {
            return new DateTime(date.Year, month, 1).ToString("Y", culture);
        }

        int day = date.Day.Value;
        if (day < 1 || day > DateTime.DaysInMonth(date.Year, month))
        {
            return new DateTime(date.Year, month, 1).ToString("Y", culture);
        }

        return new DateTime(date.Year, month, day).ToString("D", culture);
    }

    public static string ResolveMetricLabel(
        ShareSocialImageMetricKind kind,
        ShareSocialImageLocalizedCopy copy)
    {
        return copy.MetricLabels.TryGetValue(kind, out string? label)
            ? label
            : string.Empty;
    }

    public static string FormatMetric(ShareSocialImageMetric metric, CultureInfo culture)
    {
        return metric.Kind == ShareSocialImageMetricKind.Rating
            ? $"{metric.Value.ToString("0.0", culture)} / 5"
            : metric.Value.ToString("N0", culture);
    }

    public static string FitText(
        string value,
        Font font,
        float maximumWidth,
        IReadOnlyList<FontFamily> fallbackFontFamilies)
    {
        string normalizedValue = value?.Trim() ?? string.Empty;
        if (normalizedValue.Length == 0)
        {
            return string.Empty;
        }

        TextOptions options = new TextOptions(font)
        {
            FallbackFontFamilies = fallbackFontFamilies,
        };
        if (TextMeasurer.MeasureSize(normalizedValue, options).Width <= maximumWidth)
        {
            return normalizedValue;
        }

        int[] textElementIndexes = StringInfo.ParseCombiningCharacters(normalizedValue);
        for (int textElementCount = textElementIndexes.Length - 1;
             textElementCount > 0;
             textElementCount--)
        {
            string candidate = normalizedValue[..textElementIndexes[textElementCount]].TrimEnd();
            string truncated = $"{candidate}…";
            if (TextMeasurer.MeasureSize(truncated, options).Width <= maximumWidth)
            {
                return truncated;
            }
        }

        return "…";
    }
}
