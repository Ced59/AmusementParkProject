using AmusementPark.Core.Domain.TechnicalPages;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.TechnicalPages;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Mappers;

internal static partial class EntityMongoMappers
{
    public static TechnicalPage ToDomain(this TechnicalPageDocument document)
    {
        TechnicalPage page = new TechnicalPage
        {
            Id = document.Id,
            CategoryKey = document.CategoryKey,
            CategoryNames = CommonMongoMappers.ToDomain(document.CategoryNames),
            Slug = document.Slug,
            Titles = CommonMongoMappers.ToDomain(document.Titles),
            Summaries = CommonMongoMappers.ToDomain(document.Summaries),
            Aliases = document.Aliases.Select(ToDomain).ToList(),
            ContentBlocks = document.ContentBlocks.Select(ToDomain).ToList(),
            SortOrder = document.SortOrder,
            IsVisible = document.IsVisible,
            AdminReviewStatus = document.AdminReviewStatus.NormalizeForAdministration(),
        };
        page.CreatedAtUtc = document.CreatedAt;
        page.UpdatedAtUtc = document.UpdatedAt;
        return page;
    }

    public static TechnicalPageDocument ToDocument(this TechnicalPage page)
    {
        return new TechnicalPageDocument
        {
            Id = page.Id,
            CategoryKey = page.CategoryKey,
            CategoryNames = CommonMongoMappers.ToDocuments(page.CategoryNames),
            Slug = page.Slug,
            Titles = CommonMongoMappers.ToDocuments(page.Titles),
            Summaries = CommonMongoMappers.ToDocuments(page.Summaries),
            Aliases = page.Aliases.Select(ToDocument).ToList(),
            ContentBlocks = page.ContentBlocks.Select(ToDocument).ToList(),
            SortOrder = page.SortOrder,
            IsVisible = page.IsVisible,
            AdminReviewStatus = page.AdminReviewStatus.NormalizeForAdministration(),
            AdminReviewPriority = page.AdminReviewStatus.ToAdminReviewPriority(),
            CreatedAt = page.CreatedAtUtc,
            UpdatedAt = page.UpdatedAtUtc,
        };
    }

    private static TechnicalPageAlias ToDomain(TechnicalPageAliasDocument document)
    {
        return new TechnicalPageAlias
        {
            CategoryKey = document.CategoryKey,
            Labels = CommonMongoMappers.ToDomain(document.Labels),
        };
    }

    private static TechnicalPageAliasDocument ToDocument(TechnicalPageAlias alias)
    {
        return new TechnicalPageAliasDocument
        {
            CategoryKey = alias.CategoryKey,
            Labels = CommonMongoMappers.ToDocuments(alias.Labels),
        };
    }

    private static TechnicalContentBlock ToDomain(TechnicalContentBlockDocument document)
    {
        return new TechnicalContentBlock
        {
            BlockType = document.BlockType,
            Tone = document.Tone,
            ImageUrl = document.ImageUrl,
            ImageId = document.ImageId,
            DiagramKey = document.DiagramKey,
            Titles = CommonMongoMappers.ToDomain(document.Titles),
            Bodies = CommonMongoMappers.ToDomain(document.Bodies),
            Captions = CommonMongoMappers.ToDomain(document.Captions),
            AltTexts = CommonMongoMappers.ToDomain(document.AltTexts),
            Items = document.Items.Select(ToDomain).ToList(),
            Table = document.Table is null ? null : ToDomain(document.Table),
            Metrics = document.Metrics.Select(ToDomain).ToList(),
            Links = document.Links.Select(ToDomain).ToList(),
            Columns = document.Columns.Select(ToDomain).ToList(),
        };
    }

    private static TechnicalContentBlockDocument ToDocument(TechnicalContentBlock block)
    {
        return new TechnicalContentBlockDocument
        {
            BlockType = block.BlockType,
            Tone = block.Tone,
            ImageUrl = block.ImageUrl,
            ImageId = block.ImageId,
            DiagramKey = block.DiagramKey,
            Titles = CommonMongoMappers.ToDocuments(block.Titles),
            Bodies = CommonMongoMappers.ToDocuments(block.Bodies),
            Captions = CommonMongoMappers.ToDocuments(block.Captions),
            AltTexts = CommonMongoMappers.ToDocuments(block.AltTexts),
            Items = block.Items.Select(ToDocument).ToList(),
            Table = block.Table is null ? null : ToDocument(block.Table),
            Metrics = block.Metrics.Select(ToDocument).ToList(),
            Links = block.Links.Select(ToDocument).ToList(),
            Columns = block.Columns.Select(ToDocument).ToList(),
        };
    }

    private static TechnicalContentListItem ToDomain(TechnicalContentListItemDocument document)
    {
        return new TechnicalContentListItem
        {
            Texts = CommonMongoMappers.ToDomain(document.Texts),
        };
    }

    private static TechnicalContentListItemDocument ToDocument(TechnicalContentListItem item)
    {
        return new TechnicalContentListItemDocument
        {
            Texts = CommonMongoMappers.ToDocuments(item.Texts),
        };
    }

    private static TechnicalContentTable ToDomain(TechnicalContentTableDocument document)
    {
        return new TechnicalContentTable
        {
            Headers = document.Headers.Select(ToDomain).ToList(),
            Rows = document.Rows.Select(ToDomain).ToList(),
        };
    }

    private static TechnicalContentTableDocument ToDocument(TechnicalContentTable table)
    {
        return new TechnicalContentTableDocument
        {
            Headers = table.Headers.Select(ToDocument).ToList(),
            Rows = table.Rows.Select(ToDocument).ToList(),
        };
    }

    private static TechnicalContentTableRow ToDomain(TechnicalContentTableRowDocument document)
    {
        return new TechnicalContentTableRow
        {
            Cells = document.Cells.Select(ToDomain).ToList(),
        };
    }

    private static TechnicalContentTableRowDocument ToDocument(TechnicalContentTableRow row)
    {
        return new TechnicalContentTableRowDocument
        {
            Cells = row.Cells.Select(ToDocument).ToList(),
        };
    }

    private static TechnicalContentTableCell ToDomain(TechnicalContentTableCellDocument document)
    {
        return new TechnicalContentTableCell
        {
            Texts = CommonMongoMappers.ToDomain(document.Texts),
        };
    }

    private static TechnicalContentTableCellDocument ToDocument(TechnicalContentTableCell cell)
    {
        return new TechnicalContentTableCellDocument
        {
            Texts = CommonMongoMappers.ToDocuments(cell.Texts),
        };
    }

    private static TechnicalContentMetric ToDomain(TechnicalContentMetricDocument document)
    {
        return new TechnicalContentMetric
        {
            Label = CommonMongoMappers.ToDomain(document.Label),
            Value = CommonMongoMappers.ToDomain(document.Value),
            HelpText = CommonMongoMappers.ToDomain(document.HelpText),
        };
    }

    private static TechnicalContentMetricDocument ToDocument(TechnicalContentMetric metric)
    {
        return new TechnicalContentMetricDocument
        {
            Label = CommonMongoMappers.ToDocuments(metric.Label),
            Value = CommonMongoMappers.ToDocuments(metric.Value),
            HelpText = CommonMongoMappers.ToDocuments(metric.HelpText),
        };
    }

    private static TechnicalContentLink ToDomain(TechnicalContentLinkDocument document)
    {
        return new TechnicalContentLink
        {
            Url = document.Url,
            Label = CommonMongoMappers.ToDomain(document.Label),
        };
    }

    private static TechnicalContentLinkDocument ToDocument(TechnicalContentLink link)
    {
        return new TechnicalContentLinkDocument
        {
            Url = link.Url,
            Label = CommonMongoMappers.ToDocuments(link.Label),
        };
    }
}
