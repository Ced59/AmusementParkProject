using AmusementPark.Application.Features.ParkGraphUpserts.Contracts;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Services;

internal static class ParkGraphOfficialMapExportMapper
{
    public static List<ParkGraphExportOfficialMap> Map(IReadOnlyCollection<ParkOfficialMap> officialMaps)
    {
        return officialMaps
            .OrderByDescending(static officialMap => officialMap.Year)
            .ThenBy(static officialMap => officialMap.LanguageCode, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static officialMap => officialMap.Format)
            .ThenBy(static officialMap => officialMap.Id, StringComparer.Ordinal)
            .Select(static officialMap => new ParkGraphExportOfficialMap
            {
                Key = officialMap.Id,
                Id = officialMap.Id,
                Year = officialMap.Year,
                Format = officialMap.Format,
                DocumentUrl = officialMap.DocumentUrl,
                StorageKey = officialMap.StorageKey,
                OriginalFileName = officialMap.OriginalFileName,
                ContentType = officialMap.ContentType,
                SizeInBytes = officialMap.SizeInBytes,
                PreviewImageUrl = officialMap.PreviewImageUrl,
                SourcePageUrl = officialMap.SourcePageUrl,
                LanguageCode = officialMap.LanguageCode,
                Titles = CopyLocalizedTexts(officialMap.Titles),
                AlternativeTexts = CopyLocalizedTexts(officialMap.AlternativeTexts),
                IsVisible = officialMap.IsVisible,
                LastVerifiedAtUtc = officialMap.LastVerifiedAtUtc,
            })
            .ToList();
    }

    private static List<LocalizedText> CopyLocalizedTexts(IReadOnlyCollection<LocalizedText> values)
    {
        return values
            .Select(static value => new LocalizedText(value.LanguageCode, value.Value))
            .ToList();
    }
}
