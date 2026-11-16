using System.Text.RegularExpressions;
using AmusementPark.WebAPI.Contracts.Common;
using AmusementPark.WebAPI.Contracts.Home;
using AmusementPark.WebAPI.Contracts.Parks;
using AmusementPark.WebAPI.Contracts.AttractionManufacturers;

namespace AmusementPark.WebAPI.Mappers;

/// <summary>Opt-in transport projections; only newly mapped DTOs are changed, never domain entities.</summary>
public static class PublicCardProjectionHttpMapper
{
    public static ParkDto ToCardHttp(this ParkDto value)
    {
        value.ShortDescriptions = Format(value.Descriptions, 140, "...");
        value.Descriptions = new();
        return value;
    }

    public static HomeFeaturedParkDto ToCardHttp(this HomeFeaturedParkDto value)
    {
        value.ShortDescriptions = Format(value.Descriptions, 142, "…");
        value.Descriptions = new();
        return value;
    }

    public static HomeLatestArticleDto ToCardHttp(this HomeLatestArticleDto value)
    {
        value.ShortSummaries = Format(value.Summaries, 142, "…");
        value.Summaries = new();
        return value;
    }

    public static AttractionManufacturerDto ToCardHttp(this AttractionManufacturerDto value)
    {
        // resolveLocalizedText selects the first displayable translation, unlike resolveLocalizedValue.
        value.BiographyPreviews = value.Biography.Select(text => new
        {
            Text = text,
            Preview = PublicCardTextHttpMapper.ToCardDescription(text.Value, 180, "...", false),
        }).Where(static item => item.Preview is not null || Regex.IsMatch(item.Text.Value ?? string.Empty, @"<[iI][mM][gG](?=[^A-Za-z0-9_]|$)"))
        .Select(static item => new LocalizedTextDto { LanguageCode = item.Text.LanguageCode, Value = item.Preview }).ToList();
        value.Biography = new();
        return value;
    }

    private static List<LocalizedTextDto> Format(IEnumerable<LocalizedTextDto> texts, int length, string ellipsis, bool natural = true)
    {
        return texts.Select(text => new LocalizedTextDto
        {
            LanguageCode = text.LanguageCode,
            Value = PublicCardTextHttpMapper.ToCardDescription(text.Value, length, ellipsis, natural),
        }).ToList();
    }
}
