using System.Text.RegularExpressions;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Contact.Commands;
using AmusementPark.Application.Features.Contact.Contracts;
using AmusementPark.Application.Features.Contact.Ports;
using AmusementPark.Core.Domain.Contact;

namespace AmusementPark.Application.Features.Contact.Handlers;

public sealed class SubmitContactGrievanceCommandHandler
    : ICommandHandler<SubmitContactGrievanceCommand, ApplicationResult<ContactGrievanceSubmissionResult>>
{
    private const int MinimumMessageLength = 10;
    private const int MaximumMessageLength = 2000;
    private const int MaximumNewLineCount = 40;
    private const int MaximumUrlCount = 3;
    private const int MaximumRecentSubmissionsByIp = 3;
    private static readonly TimeSpan RecentSubmissionWindow = TimeSpan.FromMinutes(15);
    private static readonly Regex UrlRegex = new Regex(@"(?:https?://|www\.)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private readonly IContactGrievanceRepository repository;
    private readonly IContactNotificationService notificationService;

    public SubmitContactGrievanceCommandHandler(
        IContactGrievanceRepository repository,
        IContactNotificationService notificationService)
    {
        this.repository = repository;
        this.notificationService = notificationService;
    }

    public async Task<ApplicationResult<ContactGrievanceSubmissionResult>> HandleAsync(
        SubmitContactGrievanceCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        ContactGrievanceSubmission submission = command.Submission;
        if (!string.IsNullOrWhiteSpace(submission.Honeypot))
        {
            return ApplicationResult<ContactGrievanceSubmissionResult>.Success(new ContactGrievanceSubmissionResult(true, null));
        }

        string normalizedMessage = NormalizeMessage(submission.Message);
        IReadOnlyDictionary<string, IReadOnlyCollection<string>> validationErrors = ValidateMessage(normalizedMessage);
        if (validationErrors.Count > 0)
        {
            return ApplicationResult<ContactGrievanceSubmissionResult>.Failure(ContactApplicationErrors.InvalidSubmission(validationErrors));
        }

        string normalizedIpAddress = NormalizeIpAddress(submission.IpAddress);
        if (!string.Equals(normalizedIpAddress, "unknown", StringComparison.OrdinalIgnoreCase))
        {
            DateTime submittedSinceUtc = DateTime.UtcNow.Subtract(RecentSubmissionWindow);
            long recentSubmissionCount = await this.repository.CountRecentByIpAsync(normalizedIpAddress, submittedSinceUtc, cancellationToken);
            if (recentSubmissionCount >= MaximumRecentSubmissionsByIp)
            {
                return ApplicationResult<ContactGrievanceSubmissionResult>.Failure(ContactApplicationErrors.TooManySubmissions());
            }
        }

        ContactGrievance grievance = new ContactGrievance
        {
            Message = normalizedMessage,
            LanguageCode = NormalizeLanguageCode(submission.LanguageCode),
            IpAddress = normalizedIpAddress,
            UserAgent = NormalizeUserAgent(submission.UserAgent),
        };

        ContactGrievance created = await this.repository.CreateAsync(grievance, cancellationToken);
        await this.notificationService.NotifySubmittedAsync(created, cancellationToken);
        return ApplicationResult<ContactGrievanceSubmissionResult>.Success(new ContactGrievanceSubmissionResult(true, created.CreatedAtUtc));
    }

    private static IReadOnlyDictionary<string, IReadOnlyCollection<string>> ValidateMessage(string message)
    {
        Dictionary<string, IReadOnlyCollection<string>> errors = new Dictionary<string, IReadOnlyCollection<string>>();

        if (message.Length < MinimumMessageLength)
        {
            errors["message"] = new[] { "Le message doit contenir au moins 10 caracteres." };
            return errors;
        }

        if (message.Length > MaximumMessageLength)
        {
            errors["message"] = new[] { "Le message ne doit pas depasser 2000 caracteres." };
            return errors;
        }

        if (ContainsForbiddenControlCharacter(message))
        {
            errors["message"] = new[] { "Le message contient des caracteres non autorises." };
            return errors;
        }

        if (message.Contains('<', StringComparison.Ordinal) || message.Contains('>', StringComparison.Ordinal))
        {
            errors["message"] = new[] { "Le message doit rester en texte simple, sans balises." };
            return errors;
        }

        if (message.Count(static character => character == '\n') > MaximumNewLineCount)
        {
            errors["message"] = new[] { "Le message contient trop de retours a la ligne." };
            return errors;
        }

        if (UrlRegex.Matches(message).Count > MaximumUrlCount)
        {
            errors["message"] = new[] { "Le message contient trop de liens." };
        }

        return errors;
    }

    private static string NormalizeMessage(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return string.Empty;
        }

        return message.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace("\r", "\n", StringComparison.Ordinal)
            .Trim();
    }

    private static bool ContainsForbiddenControlCharacter(string message)
    {
        return message.Any(static character => char.IsControl(character) && character != '\n' && character != '\t');
    }

    private static string NormalizeIpAddress(string? ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            return "unknown";
        }

        string normalizedIpAddress = ipAddress.Trim();
        return normalizedIpAddress.Length <= 64 ? normalizedIpAddress : normalizedIpAddress[..64];
    }

    private static string? NormalizeUserAgent(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
        {
            return null;
        }

        string normalizedUserAgent = userAgent.Trim();
        return normalizedUserAgent.Length <= 256 ? normalizedUserAgent : normalizedUserAgent[..256];
    }

    private static string? NormalizeLanguageCode(string? languageCode)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
        {
            return null;
        }

        string normalizedLanguageCode = languageCode.Trim().ToLowerInvariant();
        return normalizedLanguageCode.Length >= 2 ? normalizedLanguageCode[..2] : null;
    }
}
