using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Contracts;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Comments.Commands;
using AmusementPark.Application.Features.Comments.Ports;
using AmusementPark.Application.Features.Comments.Queries;
using AmusementPark.Application.Features.Comments.Results;
using AmusementPark.Application.Features.Comments.Services;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Core.Domain.Comments;
using AmusementPark.Core.Domain.Users;
using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.Comments.Handlers;

internal static class CommentBodyNormalizer
{
    private const int MaximumBodyLength = 12000;

    public static ApplicationResult<IReadOnlyCollection<LocalizedText>> Normalize(
        IReadOnlyCollection<LocalizedTextValue>? values,
        ICommentContentSanitizer contentSanitizer)
    {
        Dictionary<string, LocalizedText> normalized =
            new Dictionary<string, LocalizedText>(StringComparer.OrdinalIgnoreCase);
        bool hasPlainText = false;

        foreach (LocalizedTextValue value in values ?? Array.Empty<LocalizedTextValue>())
        {
            string languageCode = value.LanguageCode?.Trim().ToLowerInvariant() ?? string.Empty;
            if (!CommentLanguageCodes.IsSupported(languageCode))
            {
                return ApplicationResult<IReadOnlyCollection<LocalizedText>>.Failure(
                    CommentApplicationErrors.InvalidLanguage());
            }

            if (value.Value.Length > MaximumBodyLength)
            {
                return ApplicationResult<IReadOnlyCollection<LocalizedText>>.Failure(
                    CommentApplicationErrors.BodyTooLong());
            }

            string sanitizedValue = contentSanitizer.SanitizeRichHtml(value.Value);
            string plainText = contentSanitizer.ExtractPlainText(sanitizedValue);
            bool hasImage = string.IsNullOrWhiteSpace(plainText)
                && contentSanitizer.ExtractImageIds(sanitizedValue).Count > 0;
            if (string.IsNullOrWhiteSpace(plainText) && !hasImage)
            {
                continue;
            }

            hasPlainText |= !string.IsNullOrWhiteSpace(plainText);

            if (sanitizedValue.Length > MaximumBodyLength)
            {
                return ApplicationResult<IReadOnlyCollection<LocalizedText>>.Failure(
                    CommentApplicationErrors.BodyTooLong());
            }

            normalized[languageCode] = new LocalizedText(languageCode, sanitizedValue);
        }

        if (normalized.Count == 0 || !hasPlainText)
        {
            return ApplicationResult<IReadOnlyCollection<LocalizedText>>.Failure(
                CommentApplicationErrors.EmptyBody());
        }

        return ApplicationResult<IReadOnlyCollection<LocalizedText>>.Success(
            normalized.Values.OrderBy(static value => value.LanguageCode, StringComparer.Ordinal).ToList());
    }
}
