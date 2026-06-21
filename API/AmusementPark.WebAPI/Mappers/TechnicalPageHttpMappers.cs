using AmusementPark.Application.Features.TechnicalPages.Results;
using AmusementPark.Core.Domain.TechnicalPages;
using AmusementPark.WebAPI.Contracts.TechnicalPages;

namespace AmusementPark.WebAPI.Mappers;

internal static class TechnicalPageHttpMappers
{
    public static TechnicalPage ToDomain(this TechnicalPageDto dto)
    {
        TechnicalPage page = new TechnicalPage
        {
            Id = dto.Id ?? string.Empty,
            CategoryKey = dto.CategoryKey,
            CategoryNames = dto.CategoryNames.ToDomain(),
            Slug = dto.Slug,
            Titles = dto.Titles.ToDomain(),
            Summaries = dto.Summaries.ToDomain(),
            Aliases = dto.Aliases.Select(ToDomain).ToList(),
            ContentBlocks = dto.ContentBlocks.Select(ToDomain).ToList(),
            SortOrder = dto.SortOrder,
            IsVisible = dto.IsVisible,
            AdminReviewStatus = dto.AdminReviewStatus.ToDomain(),
        };
        return page;
    }

    public static TechnicalPageDto ToHttp(this TechnicalPageResult result)
    {
        return new TechnicalPageDto
        {
            Id = result.Id,
            CategoryKey = result.CategoryKey,
            CategoryNames = result.CategoryNames.ToHttp(),
            Slug = result.Slug,
            Titles = result.Titles.ToHttp(),
            Summaries = result.Summaries.ToHttp(),
            Aliases = result.Aliases.Select(ToHttp).ToList(),
            ContentBlocks = result.ContentBlocks.Select(ToHttp).ToList(),
            SortOrder = result.SortOrder,
            IsVisible = result.IsVisible,
            AdminReviewStatus = result.AdminReviewStatus.ToHttp(),
            UpdatedAtUtc = result.UpdatedAtUtc,
        };
    }

    public static TechnicalPagesJsonUpsertResultDto ToHttp(this TechnicalPageJsonUpsertResult result)
    {
        return new TechnicalPagesJsonUpsertResultDto
        {
            CreatedCount = result.CreatedCount,
            UpdatedCount = result.UpdatedCount,
            Pages = result.Pages.Select(ToHttp).ToList(),
        };
    }

    private static TechnicalPageAlias ToDomain(TechnicalPageAliasDto dto)
    {
        return new TechnicalPageAlias
        {
            CategoryKey = dto.CategoryKey,
            Labels = dto.Labels.ToDomain(),
        };
    }

    private static TechnicalPageAliasDto ToHttp(TechnicalPageAlias alias)
    {
        return new TechnicalPageAliasDto
        {
            CategoryKey = alias.CategoryKey,
            Labels = alias.Labels.ToHttp(),
        };
    }

    private static TechnicalContentBlock ToDomain(TechnicalContentBlockDto dto)
    {
        return new TechnicalContentBlock
        {
            BlockType = dto.BlockType,
            Tone = dto.Tone,
            ImageUrl = dto.ImageUrl,
            ImageId = dto.ImageId,
            DiagramKey = dto.DiagramKey,
            Titles = dto.Titles.ToDomain(),
            Bodies = dto.Bodies.ToDomain(),
            Captions = dto.Captions.ToDomain(),
            AltTexts = dto.AltTexts.ToDomain(),
            Items = dto.Items.Select(ToDomain).ToList(),
            Table = dto.Table is null ? null : ToDomain(dto.Table),
            Metrics = dto.Metrics.Select(ToDomain).ToList(),
            Links = dto.Links.Select(ToDomain).ToList(),
            Columns = dto.Columns.Select(ToDomain).ToList(),
        };
    }

    private static TechnicalContentBlockDto ToHttp(TechnicalContentBlock block)
    {
        return new TechnicalContentBlockDto
        {
            BlockType = block.BlockType,
            Tone = block.Tone,
            ImageUrl = block.ImageUrl,
            ImageId = block.ImageId,
            DiagramKey = block.DiagramKey,
            Titles = block.Titles.ToHttp(),
            Bodies = block.Bodies.ToHttp(),
            Captions = block.Captions.ToHttp(),
            AltTexts = block.AltTexts.ToHttp(),
            Items = block.Items.Select(ToHttp).ToList(),
            Table = block.Table is null ? null : ToHttp(block.Table),
            Metrics = block.Metrics.Select(ToHttp).ToList(),
            Links = block.Links.Select(ToHttp).ToList(),
            Columns = block.Columns.Select(ToHttp).ToList(),
        };
    }

    private static TechnicalContentListItem ToDomain(TechnicalContentListItemDto dto)
    {
        return new TechnicalContentListItem
        {
            Texts = dto.Texts.ToDomain(),
        };
    }

    private static TechnicalContentListItemDto ToHttp(TechnicalContentListItem item)
    {
        return new TechnicalContentListItemDto
        {
            Texts = item.Texts.ToHttp(),
        };
    }

    private static TechnicalContentTable ToDomain(TechnicalContentTableDto dto)
    {
        return new TechnicalContentTable
        {
            Headers = dto.Headers.Select(ToDomain).ToList(),
            Rows = dto.Rows.Select(ToDomain).ToList(),
        };
    }

    private static TechnicalContentTableDto ToHttp(TechnicalContentTable table)
    {
        return new TechnicalContentTableDto
        {
            Headers = table.Headers.Select(ToHttp).ToList(),
            Rows = table.Rows.Select(ToHttp).ToList(),
        };
    }

    private static TechnicalContentTableRow ToDomain(TechnicalContentTableRowDto dto)
    {
        return new TechnicalContentTableRow
        {
            Cells = dto.Cells.Select(ToDomain).ToList(),
        };
    }

    private static TechnicalContentTableRowDto ToHttp(TechnicalContentTableRow row)
    {
        return new TechnicalContentTableRowDto
        {
            Cells = row.Cells.Select(ToHttp).ToList(),
        };
    }

    private static TechnicalContentTableCell ToDomain(TechnicalContentTableCellDto dto)
    {
        return new TechnicalContentTableCell
        {
            Texts = dto.Texts.ToDomain(),
        };
    }

    private static TechnicalContentTableCellDto ToHttp(TechnicalContentTableCell cell)
    {
        return new TechnicalContentTableCellDto
        {
            Texts = cell.Texts.ToHttp(),
        };
    }

    private static TechnicalContentMetric ToDomain(TechnicalContentMetricDto dto)
    {
        return new TechnicalContentMetric
        {
            Label = dto.Label.ToDomain(),
            Value = dto.Value.ToDomain(),
            HelpText = dto.HelpText.ToDomain(),
        };
    }

    private static TechnicalContentMetricDto ToHttp(TechnicalContentMetric metric)
    {
        return new TechnicalContentMetricDto
        {
            Label = metric.Label.ToHttp(),
            Value = metric.Value.ToHttp(),
            HelpText = metric.HelpText.ToHttp(),
        };
    }

    private static TechnicalContentLink ToDomain(TechnicalContentLinkDto dto)
    {
        return new TechnicalContentLink
        {
            Url = dto.Url,
            Label = dto.Label.ToDomain(),
        };
    }

    private static TechnicalContentLinkDto ToHttp(TechnicalContentLink link)
    {
        return new TechnicalContentLinkDto
        {
            Url = link.Url,
            Label = link.Label.ToHttp(),
        };
    }
}
