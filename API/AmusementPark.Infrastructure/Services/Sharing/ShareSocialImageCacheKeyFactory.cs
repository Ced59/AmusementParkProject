using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using AmusementPark.Application.Features.Sharing.Models;

namespace AmusementPark.Infrastructure.Services.Sharing;

internal static class ShareSocialImageCacheKeyFactory
{
    public static string Create(ShareSocialImageModel model)
    {
        StringBuilder content = new StringBuilder();
        Append(content, ShareSocialImageTemplate.Version.ToString(CultureInfo.InvariantCulture));
        Append(content, model.PublicationType.ToString());
        Append(content, model.PublicationVersion.ToString(CultureInfo.InvariantCulture));
        Append(content, model.Language);
        Append(content, model.Subject ?? string.Empty);
        Append(content, model.Year?.ToString(CultureInfo.InvariantCulture) ?? string.Empty);
        if (model.Date is not null)
        {
            Append(content, model.Date.Year.ToString(CultureInfo.InvariantCulture));
            Append(content, model.Date.Month?.ToString(CultureInfo.InvariantCulture) ?? string.Empty);
            Append(content, model.Date.Day?.ToString(CultureInfo.InvariantCulture) ?? string.Empty);
            Append(content, model.Date.Precision.ToString());
        }

        foreach (ShareSocialImageMetric metric in model.Metrics.Take(3))
        {
            Append(content, metric.Kind.ToString());
            Append(content, metric.Value.ToString("R", CultureInfo.InvariantCulture));
        }

        Append(content, model.Highlight ?? string.Empty);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content.ToString())));
    }

    private static void Append(StringBuilder content, string value)
    {
        content.Append(value.Length)
            .Append(':')
            .Append(value);
    }
}
