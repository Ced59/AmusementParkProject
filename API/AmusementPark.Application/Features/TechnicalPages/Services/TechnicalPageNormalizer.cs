using System.Text;
using System.Text.RegularExpressions;
using AmusementPark.Application.Errors;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.TechnicalPages;
using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.TechnicalPages.Services;

internal static partial class TechnicalPageNormalizer
{
    private static readonly IReadOnlyCollection<string> RequiredLanguages = new[] { "fr", "en", "de", "nl", "it", "es", "pl", "pt" };

    public static ApplicationResult<TechnicalPage> NormalizeForSave(TechnicalPage? page)
    {
        if (page is null)
        {
            return ApplicationResult<TechnicalPage>.Failure(ApplicationErrors.Required(nameof(page)));
        }

        List<ApplicationError> errors = new List<ApplicationError>();
        TechnicalPage normalizedPage = new TechnicalPage
        {
            Id = page.Id,
            CategoryKey = NormalizeKey(page.CategoryKey),
            CategoryNames = NormalizeLocalizedTexts(page.CategoryNames),
            Slug = NormalizeSlug(page.Slug, page.Titles),
            Titles = NormalizeLocalizedTexts(page.Titles),
            Summaries = NormalizeLocalizedTexts(page.Summaries),
            Aliases = NormalizeAliases(page.Aliases),
            ContentBlocks = NormalizeBlocks(page.ContentBlocks),
            SortOrder = page.SortOrder,
            IsVisible = page.IsVisible,
            AdminReviewStatus = NormalizeAdminReviewStatus(page.AdminReviewStatus),
        };
        normalizedPage.CreatedAtUtc = page.CreatedAtUtc;
        normalizedPage.UpdatedAtUtc = page.UpdatedAtUtc;

        if (string.IsNullOrWhiteSpace(normalizedPage.CategoryKey))
        {
            errors.Add(ApplicationErrors.Required(nameof(page.CategoryKey)));
        }

        if (string.IsNullOrWhiteSpace(normalizedPage.Slug))
        {
            errors.Add(ApplicationErrors.Required(nameof(page.Slug)));
        }

        AddLocalizedRequiredErrors(errors, normalizedPage.CategoryNames, nameof(page.CategoryNames));
        AddLocalizedRequiredErrors(errors, normalizedPage.Titles, nameof(page.Titles));
        AddLocalizedRequiredErrors(errors, normalizedPage.Summaries, nameof(page.Summaries));

        if (errors.Count > 0)
        {
            return ApplicationResult<TechnicalPage>.Failure(errors);
        }

        return ApplicationResult<TechnicalPage>.Success(normalizedPage);
    }

    private static List<TechnicalPageAlias> NormalizeAliases(IReadOnlyCollection<TechnicalPageAlias>? aliases)
    {
        List<TechnicalPageAlias> result = new List<TechnicalPageAlias>();
        if (aliases is null)
        {
            return result;
        }

        foreach (TechnicalPageAlias alias in aliases)
        {
            string categoryKey = NormalizeKey(alias.CategoryKey);
            List<LocalizedText> labels = NormalizeLocalizedTexts(alias.Labels);
            if (string.IsNullOrWhiteSpace(categoryKey) || labels.Count == 0)
            {
                continue;
            }

            result.Add(new TechnicalPageAlias
            {
                CategoryKey = categoryKey,
                Labels = labels,
            });
        }

        return result;
    }

    private static List<TechnicalContentBlock> NormalizeBlocks(IReadOnlyCollection<TechnicalContentBlock>? blocks)
    {
        List<TechnicalContentBlock> result = new List<TechnicalContentBlock>();
        if (blocks is null)
        {
            return result;
        }

        foreach (TechnicalContentBlock block in blocks)
        {
            string blockType = NormalizeOptional(block.BlockType) ?? "richText";
            result.Add(new TechnicalContentBlock
            {
                BlockType = blockType,
                Tone = NormalizeOptional(block.Tone),
                ImageUrl = NormalizeOptional(block.ImageUrl),
                ImageId = NormalizeOptional(block.ImageId),
                DiagramKey = NormalizeOptional(block.DiagramKey),
                Titles = NormalizeLocalizedTexts(block.Titles),
                Bodies = NormalizeLocalizedTexts(block.Bodies),
                Captions = NormalizeLocalizedTexts(block.Captions),
                AltTexts = NormalizeLocalizedTexts(block.AltTexts),
                Items = NormalizeListItems(block.Items),
                Table = NormalizeTable(block.Table),
                Metrics = NormalizeMetrics(block.Metrics),
                Links = NormalizeLinks(block.Links),
                Columns = NormalizeBlocks(block.Columns),
            });
        }

        return result;
    }

    private static List<TechnicalContentListItem> NormalizeListItems(IReadOnlyCollection<TechnicalContentListItem>? items)
    {
        List<TechnicalContentListItem> result = new List<TechnicalContentListItem>();
        if (items is null)
        {
            return result;
        }

        foreach (TechnicalContentListItem item in items)
        {
            List<LocalizedText> texts = NormalizeLocalizedTexts(item.Texts);
            if (texts.Count > 0)
            {
                result.Add(new TechnicalContentListItem { Texts = texts });
            }
        }

        return result;
    }

    private static TechnicalContentTable? NormalizeTable(TechnicalContentTable? table)
    {
        if (table is null)
        {
            return null;
        }

        return new TechnicalContentTable
        {
            Headers = NormalizeTableCells(table.Headers),
            Rows = NormalizeRows(table.Rows),
        };
    }

    private static List<TechnicalContentTableCell> NormalizeTableCells(IReadOnlyCollection<TechnicalContentTableCell>? cells)
    {
        List<TechnicalContentTableCell> result = new List<TechnicalContentTableCell>();
        if (cells is null)
        {
            return result;
        }

        foreach (TechnicalContentTableCell cell in cells)
        {
            List<LocalizedText> texts = NormalizeLocalizedTexts(cell.Texts);
            if (texts.Count > 0)
            {
                result.Add(new TechnicalContentTableCell { Texts = texts });
            }
        }

        return result;
    }

    private static List<TechnicalContentTableRow> NormalizeRows(IReadOnlyCollection<TechnicalContentTableRow>? rows)
    {
        List<TechnicalContentTableRow> result = new List<TechnicalContentTableRow>();
        if (rows is null)
        {
            return result;
        }

        foreach (TechnicalContentTableRow row in rows)
        {
            List<TechnicalContentTableCell> cells = NormalizeTableCells(row.Cells);
            if (cells.Count > 0)
            {
                result.Add(new TechnicalContentTableRow { Cells = cells });
            }
        }

        return result;
    }

    private static List<TechnicalContentMetric> NormalizeMetrics(IReadOnlyCollection<TechnicalContentMetric>? metrics)
    {
        List<TechnicalContentMetric> result = new List<TechnicalContentMetric>();
        if (metrics is null)
        {
            return result;
        }

        foreach (TechnicalContentMetric metric in metrics)
        {
            result.Add(new TechnicalContentMetric
            {
                Label = NormalizeLocalizedTexts(metric.Label),
                Value = NormalizeLocalizedTexts(metric.Value),
                HelpText = NormalizeLocalizedTexts(metric.HelpText),
            });
        }

        return result;
    }

    private static List<TechnicalContentLink> NormalizeLinks(IReadOnlyCollection<TechnicalContentLink>? links)
    {
        List<TechnicalContentLink> result = new List<TechnicalContentLink>();
        if (links is null)
        {
            return result;
        }

        foreach (TechnicalContentLink link in links)
        {
            string? url = NormalizeOptional(link.Url);
            List<LocalizedText> label = NormalizeLocalizedTexts(link.Label);
            if (url is not null && label.Count > 0)
            {
                result.Add(new TechnicalContentLink
                {
                    Url = url,
                    Label = label,
                });
            }
        }

        return result;
    }

    private static List<LocalizedText> NormalizeLocalizedTexts(IReadOnlyCollection<LocalizedText>? values)
    {
        Dictionary<string, LocalizedText> result = new Dictionary<string, LocalizedText>(StringComparer.OrdinalIgnoreCase);
        if (values is null)
        {
            return new List<LocalizedText>();
        }

        foreach (LocalizedText value in values)
        {
            string languageCode = NormalizeLanguageCode(value.LanguageCode);
            string? text = NormalizeOptional(value.Value);
            if (string.IsNullOrWhiteSpace(languageCode) || text is null)
            {
                continue;
            }

            result[languageCode] = new LocalizedText(languageCode, text);
        }

        return result.Values.OrderBy(static value => value.LanguageCode, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static void AddLocalizedRequiredErrors(List<ApplicationError> errors, IReadOnlyCollection<LocalizedText> values, string fieldName)
    {
        foreach (string language in RequiredLanguages)
        {
            bool hasLanguage = values.Any(value => string.Equals(value.LanguageCode, language, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(value.Value));
            if (!hasLanguage)
            {
                errors.Add(ApplicationError.Validation("technical-page.localized.required", $"{fieldName}.{language} is required."));
            }
        }
    }

    private static string NormalizeKey(string? value)
    {
        string normalizedValue = value?.Trim().ToLowerInvariant() ?? string.Empty;
        if (normalizedValue.Length == 0)
        {
            return string.Empty;
        }

        return KeyCleanupRegex().Replace(normalizedValue, "-").Trim('-');
    }

    private static string NormalizeSlug(string? slug, IReadOnlyCollection<LocalizedText>? titles)
    {
        string? normalizedSlug = NormalizeOptional(slug);
        if (normalizedSlug is not null)
        {
            return NormalizeKey(normalizedSlug);
        }

        string? title = titles?
            .FirstOrDefault(static value => string.Equals(value.LanguageCode, "en", StringComparison.OrdinalIgnoreCase))?.Value
            ?? titles?.FirstOrDefault(static value => !string.IsNullOrWhiteSpace(value.Value))?.Value;
        return NormalizeKey(RemoveDiacritics(title));
    }

    private static string NormalizeLanguageCode(string? languageCode)
    {
        string normalizedLanguageCode = languageCode?.Trim().ToLowerInvariant() ?? string.Empty;
        return normalizedLanguageCode.Length > 2 ? normalizedLanguageCode[..2] : normalizedLanguageCode;
    }

    private static string? NormalizeOptional(string? value)
    {
        string normalizedValue = value?.Trim() ?? string.Empty;
        return normalizedValue.Length == 0 ? null : normalizedValue;
    }

    private static string? RemoveDiacritics(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string normalizedValue = value.Normalize(NormalizationForm.FormD);
        StringBuilder builder = new StringBuilder(normalizedValue.Length);
        foreach (char character in normalizedValue)
        {
            System.Globalization.UnicodeCategory unicodeCategory = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(character);
            if (unicodeCategory != System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private static AdminReviewStatus NormalizeAdminReviewStatus(AdminReviewStatus status)
    {
        return status switch
        {
            AdminReviewStatus.Validated => AdminReviewStatus.Validated,
            AdminReviewStatus.ToProcessLater => AdminReviewStatus.ToProcessLater,
            AdminReviewStatus.NotRelevant => AdminReviewStatus.NotRelevant,
            _ => AdminReviewStatus.ToReview,
        };
    }

    [GeneratedRegex("[^a-z0-9]+", RegexOptions.Compiled)]
    private static partial Regex KeyCleanupRegex();
}
