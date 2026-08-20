using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Ratings.Results;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace AmusementPark.Infrastructure.Services.Ratings;

public sealed class UserRankingSharePreviewRenderer : IUserRankingSharePreviewRenderer
{
    private const int ImageWidth = 1200;
    private const int ImageHeight = 630;
    private readonly FontFamily fontFamily;

    public UserRankingSharePreviewRenderer()
    {
        FontCollection collection = new FontCollection();
        string fontPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "Fonts", "bebas-neue-latin.ttf");
        this.fontFamily = collection.Add(fontPath);
    }

    public async Task<byte[]> RenderPngAsync(
        UserRankingSharePreviewResult preview,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(preview);

        using Image<Rgba32> image = new Image<Rgba32>(ImageWidth, ImageHeight, Color.ParseHex("070A12"));
        Font brandFont = this.fontFamily.CreateFont(38, FontStyle.Regular);
        Font nameFont = this.fontFamily.CreateFont(64, FontStyle.Regular);
        Font topFont = this.fontFamily.CreateFont(28, FontStyle.Regular);
        Font itemFont = this.fontFamily.CreateFont(29, FontStyle.Regular);
        Font detailFont = this.fontFamily.CreateFont(20, FontStyle.Regular);
        Font scoreFont = this.fontFamily.CreateFont(27, FontStyle.Regular);
        IReadOnlyCollection<UserRankingSharePreviewItemResult> items = preview.Items.Take(5).ToList();

        image.Mutate(context =>
        {
            context.Fill(
                new LinearGradientBrush(
                    new PointF(0, 0),
                    new PointF(ImageWidth, ImageHeight),
                    GradientRepetitionMode.None,
                    new[]
                    {
                        new ColorStop(0f, Color.ParseHex("111827")),
                        new ColorStop(0.58f, Color.ParseHex("07101E")),
                        new ColorStop(1f, Color.ParseHex("05070C")),
                    }),
                new RectangleF(0, 0, ImageWidth, ImageHeight));
            context.Fill(Color.FromRgba(255, 111, 0, 36), new EllipsePolygon(1080, 70, 260));
            context.Fill(Color.FromRgba(30, 200, 255, 28), new EllipsePolygon(120, 610, 300));
            context.Fill(Color.ParseHex("FF7A00"), new RectangleF(58, 50, 10, 78));
            context.DrawText("AMUSEMENTPARK.FUN", brandFont, Color.ParseHex("F8FAFC"), new PointF(88, 48));
            context.DrawText(
                FitText(preview.DisplayName, nameFont, 760),
                nameFont,
                Color.ParseHex("FFFFFF"),
                new PointF(58, 102));
            context.Fill(Color.ParseHex("DFFF00"), new RectangleF(930, 62, 204, 62));
            context.DrawText("TOP 5", topFont, Color.ParseHex("07101E"), new PointF(990, 77));

            int itemIndex = 0;
            foreach (UserRankingSharePreviewItemResult item in items)
            {
                float y = 202 + itemIndex * 78;
                Color cardColor = itemIndex == 0
                    ? Color.FromRgba(255, 122, 0, 44)
                    : Color.FromRgba(255, 255, 255, 18);
                context.Fill(cardColor, new RectangleF(58, y, 1076, 64));
                context.DrawText($"#{item.Rank}", itemFont, Color.ParseHex("FFB15C"), new PointF(82, y + 14));
                context.DrawText(
                    FitText(item.Name, itemFont, 610),
                    itemFont,
                    Color.ParseHex("F8FAFC"),
                    new PointF(158, y + 8));
                if (!string.IsNullOrWhiteSpace(item.ParkName))
                {
                    context.DrawText(
                        FitText(item.ParkName, detailFont, 610),
                        detailFont,
                        Color.ParseHex("A8B3C7"),
                        new PointF(160, y + 38));
                }

                context.DrawText(
                    $"{item.Rating:0.0} / 5",
                    scoreFont,
                    Color.ParseHex("DFFF00"),
                    new PointF(985, y + 14));
                itemIndex++;
            }

            context.DrawText(
                "AMUSEMENT-PARKS.FUN",
                detailFont,
                Color.ParseHex("7F8CA3"),
                new PointF(948, 596));
        });

        await using MemoryStream stream = new MemoryStream();
        await image.SaveAsPngAsync(stream, cancellationToken);
        return stream.ToArray();
    }

    private static string FitText(string value, Font font, float maximumWidth)
    {
        string normalizedValue = value?.Trim() ?? string.Empty;
        if (normalizedValue.Length == 0)
        {
            return "AMUSEMENTPARK";
        }

        if (TextMeasurer.MeasureSize(normalizedValue, new TextOptions(font)).Width <= maximumWidth)
        {
            return normalizedValue;
        }

        string candidate = normalizedValue;
        while (candidate.Length > 1)
        {
            candidate = candidate[..^1].TrimEnd();
            string truncated = $"{candidate}…";
            if (TextMeasurer.MeasureSize(truncated, new TextOptions(font)).Width <= maximumWidth)
            {
                return truncated;
            }
        }

        return "…";
    }
}
