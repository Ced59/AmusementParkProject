using System.Globalization;
using AmusementPark.Application.Features.Sharing.Models;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace AmusementPark.Infrastructure.Services.Sharing;

internal static class ShareSocialImageCanvasPainter
{
    private const float ContentLeft = 72f;
    private const float ContentRight = 1128f;

    public static void Draw(
        Image<Rgba32> image,
        ShareSocialImageModel model,
        ShareSocialImageLocalizedCopy copy,
        CultureInfo culture,
        FontFamily fontFamily)
    {
        string title = ShareSocialImageTextFormatter.ResolveTitle(model, copy);
        string subject = ShareSocialImageTextFormatter.ResolveSubject(model, copy);
        string context = ShareSocialImageTextFormatter.ResolveContext(model, culture);
        IReadOnlyCollection<ShareSocialImageMetric> metrics = model.Metrics.Take(3).ToList();
        Font brandFont = fontFamily.CreateFont(34, FontStyle.Regular);
        Font eyebrowFont = fontFamily.CreateFont(29, FontStyle.Regular);
        Font titleFont = fontFamily.CreateFont(66, FontStyle.Regular);
        Font contextFont = fontFamily.CreateFont(31, FontStyle.Regular);
        Font metricValueFont = fontFamily.CreateFont(48, FontStyle.Regular);
        Font metricLabelFont = fontFamily.CreateFont(22, FontStyle.Regular);
        Font highlightFont = fontFamily.CreateFont(26, FontStyle.Regular);

        image.Mutate(draw =>
        {
            DrawBackground(draw);
            draw.Fill(Color.ParseHex("FF5B24"), new RectangleF(ContentLeft, 54, 9, 67));
            draw.DrawText(
                "AMUSEMENT-PARKS.FUN",
                brandFont,
                Color.ParseHex("FFF8EA"),
                new PointF(ContentLeft + 27, 50));
            draw.DrawText(
                copy.PersonalLabel.ToUpper(culture),
                eyebrowFont,
                Color.ParseHex("D6C5A2"),
                new PointF(ContentLeft, 136));
            draw.DrawText(
                ShareSocialImageTextFormatter.FitText(
                    title,
                    titleFont,
                    ContentRight - ContentLeft),
                titleFont,
                Color.ParseHex("FFFFFF"),
                new PointF(ContentLeft, 178));
            draw.DrawText(
                ShareSocialImageTextFormatter.FitText(
                    subject,
                    contextFont,
                    context.Length > 0 ? 700 : 1000),
                contextFont,
                Color.ParseHex("FFB15C"),
                new PointF(ContentLeft, 263));

            DrawContext(draw, context, contextFont);
            DrawMetrics(draw, metrics, copy, culture, metricValueFont, metricLabelFont);
            DrawHighlight(draw, model.Highlight, copy, highlightFont);
        });
    }

    private static void DrawBackground(IImageProcessingContext draw)
    {
        draw.Fill(
            new LinearGradientBrush(
                new PointF(0, 0),
                new PointF(ShareSocialImageTemplate.Width, ShareSocialImageTemplate.Height),
                GradientRepetitionMode.None,
                new[]
                {
                    new ColorStop(0f, Color.ParseHex("1D1008")),
                    new ColorStop(0.55f, Color.ParseHex("0D0905")),
                    new ColorStop(1f, Color.ParseHex("050504")),
                }),
            new RectangleF(
                0,
                0,
                ShareSocialImageTemplate.Width,
                ShareSocialImageTemplate.Height));
        draw.Fill(Color.FromRgba(255, 91, 36, 44), new EllipsePolygon(1110, 44, 250));
        draw.Fill(Color.FromRgba(212, 255, 0, 22), new EllipsePolygon(40, 650, 330));
        draw.Draw(
            Color.FromRgba(255, 177, 92, 45),
            2f,
            new RectangularPolygon(34, 30, 1132, 570));
    }

    private static void DrawContext(
        IImageProcessingContext draw,
        string context,
        Font contextFont)
    {
        if (context.Length == 0)
        {
            return;
        }

        string fittedContext = ShareSocialImageTextFormatter.FitText(context, contextFont, 300);
        FontRectangle contextSize = TextMeasurer.MeasureSize(
            fittedContext,
            new TextOptions(contextFont));
        draw.DrawText(
            fittedContext,
            contextFont,
            Color.ParseHex("D6C5A2"),
            new PointF(ContentRight - contextSize.Width, 263));
    }

    private static void DrawMetrics(
        IImageProcessingContext draw,
        IReadOnlyCollection<ShareSocialImageMetric> metrics,
        ShareSocialImageLocalizedCopy copy,
        CultureInfo culture,
        Font valueFont,
        Font labelFont)
    {
        int count = metrics.Count;
        if (count == 0)
        {
            return;
        }

        float gap = 18f;
        float width = (ContentRight - ContentLeft - (count - 1) * gap) / count;
        int index = 0;
        foreach (ShareSocialImageMetric metric in metrics)
        {
            float x = ContentLeft + index * (width + gap);
            draw.Fill(Color.FromRgba(255, 255, 255, 15), new RectangleF(x, 331, width, 138));
            draw.Draw(
                Color.FromRgba(255, 177, 92, 55),
                1f,
                new RectangularPolygon(x, 331, width, 138));
            draw.DrawText(
                ShareSocialImageTextFormatter.FormatMetric(metric, culture),
                valueFont,
                Color.ParseHex("FFFFFF"),
                new PointF(x + 24, 349));
            draw.DrawText(
                ShareSocialImageTextFormatter.ResolveMetricLabel(metric.Kind, copy),
                labelFont,
                Color.ParseHex("D6C5A2"),
                new PointF(x + 25, 416));
            index++;
        }
    }

    private static void DrawHighlight(
        IImageProcessingContext draw,
        string? highlight,
        ShareSocialImageLocalizedCopy copy,
        Font font)
    {
        string normalizedHighlight = highlight?.Trim() ?? string.Empty;
        if (normalizedHighlight.Length == 0)
        {
            return;
        }

        string text = $"{copy.HighlightLabel} · {normalizedHighlight}";
        draw.DrawText(
            ShareSocialImageTextFormatter.FitText(text, font, 820),
            font,
            Color.ParseHex("DFFF00"),
            new PointF(ContentLeft, 510));
    }
}
