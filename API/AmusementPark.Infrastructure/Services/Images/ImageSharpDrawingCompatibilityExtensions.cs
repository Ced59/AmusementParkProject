using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;

namespace AmusementPark.Infrastructure.Services.Images;

internal static class ImageSharpDrawingCompatibilityExtensions
{
    public static void Fill(this DrawingCanvas canvas, Color color, RectangleF rectangle)
    {
        canvas.Fill(Brushes.Solid(color), new RectanglePolygon(rectangle));
    }

    public static void Fill(this DrawingCanvas canvas, Brush brush, RectangleF rectangle)
    {
        canvas.Fill(brush, new RectanglePolygon(rectangle));
    }

    public static void Fill(this DrawingCanvas canvas, Color color, IPath path)
    {
        canvas.Fill(Brushes.Solid(color), path);
    }

    public static void Draw(this DrawingCanvas canvas, Color color, float width, IPath path)
    {
        canvas.Draw(Pens.Solid(color, width), path);
    }

    public static void DrawText(
        this DrawingCanvas canvas,
        string text,
        Font font,
        Color color,
        PointF origin)
    {
        RichTextOptions options = new RichTextOptions(font)
        {
            Origin = origin,
        };
        canvas.DrawText(options, text, Brushes.Solid(color), null);
    }

    public static void DrawText(
        this DrawingCanvas canvas,
        RichTextOptions options,
        string text,
        Color color)
    {
        canvas.DrawText(options, text, Brushes.Solid(color), null);
    }
}
