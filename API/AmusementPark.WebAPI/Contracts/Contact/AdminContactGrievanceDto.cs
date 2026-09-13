namespace AmusementPark.WebAPI.Contracts.Contact;

public sealed class AdminContactGrievanceDto
{
    public string Id { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string? LanguageCode { get; set; }

    public string IpAddress { get; set; } = string.Empty;

    public string? UserAgent { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
