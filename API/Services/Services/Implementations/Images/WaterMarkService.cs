using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;
using Services.Interfaces.Images;
using SixLabors.ImageSharp.Formats.Jpeg;

namespace Services.Implementations.Images
{
    /// <summary>
    /// Service qui applique un filigrane textuel sur une image.
    /// Utilise SixLabors.ImageSharp pour la manipulation.
    /// </summary>
    public class WatermarkService : IWaterMarkService
    {
        private readonly Font font;
        private readonly Color fontColor;
        private readonly int margin;

        /// <param name="fontFamilyName">Nom de la police à charger (doit être installée ou accessible).</param>
        /// <param name="fontSize">Taille de la police (en points).</param>
        /// <param name="fontColor">Couleur du texte du filigrane.</param>
        /// <param name="margin">Marge (en pixels) entre le texte et les bords.</param>
        public WatermarkService(
            string fontFamilyName = "Arial",
            float fontSize = 24f,
            Color? fontColor = null,
            int margin = 10)
        {
            this.fontColor = fontColor ?? Color.White;
            this.margin = margin;

            FontCollection collection = new();
            collection.AddSystemFonts();

            if (!collection.TryGet(fontFamilyName, out FontFamily family)
                && !SystemFonts.Collection.TryGet(fontFamilyName, out family))
            {
                // Fallback sur la première police du système ou exception
                family = SystemFonts.Families.Any()
                    ? SystemFonts.Families.ToList()[0]
                    : throw new ArgumentException(
                        $"Police '{fontFamilyName}' introuvable.", nameof(fontFamilyName));
            }

            font = family.CreateFont(fontSize);
        }

        public async Task<Stream> ApplyWatermarkAsync(Stream imageStream, string watermarkText)
        {
            imageStream.Position = 0;
            using Image image = await Image.LoadAsync(imageStream);

            image.Mutate(ctx =>
            {
                RichTextOptions options = new(font)
                {
                    Origin = new PointF(image.Width - margin, image.Height - margin),
                    WrappingLength = image.Width,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Bottom
                };

                ctx.DrawText(options, watermarkText, fontColor);
            });

            MemoryStream output = new MemoryStream();
            await image.SaveAsJpegAsync(output, new JpegEncoder { Quality = 85 });
            output.Position = 0;

            return output;
        }
    }
}