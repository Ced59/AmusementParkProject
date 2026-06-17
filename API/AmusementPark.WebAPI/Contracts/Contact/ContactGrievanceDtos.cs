namespace AmusementPark.WebAPI.Contracts.Contact;

public sealed class SubmitContactGrievanceRequest
{
    public string? Message { get; set; }

    public string? Website { get; set; }

    public string? LanguageCode { get; set; }
}

public sealed class ContactGrievanceSubmissionDto
{
    public bool Accepted { get; set; }

    public DateTime? SubmittedAtUtc { get; set; }
}

public sealed class AdminContactGrievanceDto
{
    public string Id { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string? LanguageCode { get; set; }

    public string IpAddress { get; set; } = string.Empty;

    public string? UserAgent { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
