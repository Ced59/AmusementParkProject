namespace AmusementPark.Application.Features.Comments.Ports;

public interface ICommentContentSanitizer
{
    string SanitizeRichHtml(string value);

    string ExtractPlainText(string value);
}
