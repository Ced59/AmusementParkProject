using System.Text.RegularExpressions;
using AmusementPark.Application.Features.Comments.Ports;
using HtmlAgilityPack;

namespace AmusementPark.Infrastructure.Services.Comments;

public sealed class CommentContentSanitizer : ICommentContentSanitizer
{
    private static readonly HashSet<string> AllowedElements = new HashSet<string>(
        new[]
        {
            "a", "b", "blockquote", "br", "code", "div", "em", "h1", "h2", "h3", "i", "li", "ol", "p",
            "pre", "s", "span", "strong", "sub", "sup", "u", "ul", "img",
        },
        StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<string> DropWithContentElements = new HashSet<string>(
        new[] { "button", "embed", "form", "iframe", "input", "object", "script", "style", "svg", "textarea" },
        StringComparer.OrdinalIgnoreCase);

    private static readonly Regex AllowedClassPattern = new Regex(
        "^(ql-(align|indent|size|direction|font|color|background)(-[a-z0-9_-]+)?|rich-text__[a-z0-9_-]+)$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex WhitespacePattern = new Regex(
        "\\s+",
        RegexOptions.CultureInvariant);

    private static readonly Regex CommentImageSourcePattern = new Regex(
        "^/images/(?<id>[a-f0-9]{32})$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly string[] CommentImageAlignments = new[] { "left", "right", "center", "full" };

    public string SanitizeRichHtml(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        HtmlDocument document = new HtmlDocument();
        document.LoadHtml($"<div data-comment-root=\"true\">{value.Trim()}</div>");
        HtmlNode? root = document.DocumentNode.SelectSingleNode("//*[@data-comment-root='true']");
        if (root is null)
        {
            return string.Empty;
        }

        this.SanitizeChildren(root);
        root.Attributes.Remove("data-comment-root");
        return root.InnerHtml.Trim();
    }

    public string ExtractPlainText(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        HtmlDocument document = new HtmlDocument();
        document.LoadHtml(value);
        string decodedText = HtmlEntity.DeEntitize(document.DocumentNode.InnerText);
        return WhitespacePattern.Replace(decodedText, " ").Trim();
    }

    public IReadOnlyCollection<string> ExtractImageIds(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Array.Empty<string>();
        }

        HtmlDocument document = new HtmlDocument();
        document.LoadHtml(value);
        return document.DocumentNode
            .Descendants("img")
            .Select(static node => node.GetAttributeValue("src", string.Empty))
            .Select(static source => CommentImageSourcePattern.Match(HtmlEntity.DeEntitize(source).Trim()))
            .Where(static match => match.Success)
            .Select(static match => match.Groups["id"].Value.ToLowerInvariant())
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    private void SanitizeChildren(HtmlNode parent)
    {
        foreach (HtmlNode node in parent.ChildNodes.ToList())
        {
            if (node.NodeType == HtmlNodeType.Comment)
            {
                node.Remove();
                continue;
            }

            if (node.NodeType == HtmlNodeType.Text)
            {
                continue;
            }

            if (node.NodeType != HtmlNodeType.Element)
            {
                node.Remove();
                continue;
            }

            string tagName = node.Name.ToLowerInvariant();
            if (DropWithContentElements.Contains(tagName))
            {
                node.Remove();
                continue;
            }

            if (!AllowedElements.Contains(tagName))
            {
                this.UnwrapNode(node);
                continue;
            }

            this.SanitizeAttributes(node, tagName);
            if (tagName == "img" && node.ParentNode is null)
            {
                continue;
            }

            this.SanitizeChildren(node);
        }
    }

    private void UnwrapNode(HtmlNode node)
    {
        HtmlNode? parent = node.ParentNode;
        if (parent is null)
        {
            node.Remove();
            return;
        }

        this.SanitizeChildren(node);
        foreach (HtmlNode child in node.ChildNodes.ToList())
        {
            parent.InsertBefore(child, node);
        }

        node.Remove();
    }

    private void SanitizeAttributes(HtmlNode node, string tagName)
    {
        if (tagName == "img")
        {
            this.SanitizeImage(node);
            return;
        }

        foreach (HtmlAttribute attribute in node.Attributes.ToList())
        {
            string attributeName = attribute.Name.ToLowerInvariant();
            if (attributeName == "class")
            {
                this.SanitizeClassAttribute(node, attribute);
                continue;
            }

            if (tagName == "a" && attributeName == "href")
            {
                if (!IsSafeLink(attribute.Value))
                {
                    node.Attributes.Remove(attribute);
                }

                continue;
            }

            node.Attributes.Remove(attribute);
        }

        if (tagName == "a" && node.Attributes["href"] is not null)
        {
            node.SetAttributeValue("target", "_blank");
            node.SetAttributeValue("rel", "noopener noreferrer nofollow");
        }
    }

    private void SanitizeImage(HtmlNode node)
    {
        string source = HtmlEntity.DeEntitize(node.GetAttributeValue("src", string.Empty)).Trim();
        Match sourceMatch = CommentImageSourcePattern.Match(source);
        if (!sourceMatch.Success)
        {
            node.Remove();
            return;
        }

        string classes = node.GetAttributeValue("class", string.Empty);
        string alignment = CommentImageAlignments.FirstOrDefault(candidate =>
                classes.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Contains($"rich-text__image--{candidate}", StringComparer.OrdinalIgnoreCase))
            ?? "full";
        string alt = HtmlEntity.DeEntitize(node.GetAttributeValue("alt", string.Empty)).Trim();
        if (alt.Length > 240)
        {
            alt = alt[..240];
        }

        node.Attributes.RemoveAll();
        node.SetAttributeValue("src", $"/images/{sourceMatch.Groups["id"].Value.ToLowerInvariant()}");
        node.SetAttributeValue("alt", HtmlEntity.Entitize(alt));
        node.SetAttributeValue("class", $"rich-text__image rich-text__image--{alignment}");
    }

    private void SanitizeClassAttribute(HtmlNode node, HtmlAttribute attribute)
    {
        List<string> safeClasses = attribute.Value
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(static className => AllowedClassPattern.IsMatch(className))
            .ToList();

        if (safeClasses.Count == 0)
        {
            node.Attributes.Remove(attribute);
            return;
        }

        attribute.Value = string.Join(" ", safeClasses);
    }

    private static bool IsSafeLink(string value)
    {
        string normalizedValue = HtmlEntity.DeEntitize(value).Trim();
        if (string.IsNullOrWhiteSpace(normalizedValue))
        {
            return false;
        }

        if (normalizedValue.StartsWith("/", StringComparison.Ordinal)
            || normalizedValue.StartsWith("#", StringComparison.Ordinal))
        {
            return !normalizedValue.StartsWith("//", StringComparison.Ordinal);
        }

        if (!Uri.TryCreate(normalizedValue, UriKind.Absolute, out Uri? uri))
        {
            return false;
        }

        return uri.Scheme == Uri.UriSchemeHttp
            || uri.Scheme == Uri.UriSchemeHttps
            || uri.Scheme == Uri.UriSchemeMailto
            || uri.Scheme == "tel";
    }
}
