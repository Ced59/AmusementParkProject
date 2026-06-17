using AmusementPark.Core.Abstractions;

namespace AmusementPark.Core.Domain.Contact;

public sealed class ContactGrievance : AuditableEntity
{
    public string Message { get; set; } = string.Empty;

    public string? LanguageCode { get; set; }

    public string IpAddress { get; set; } = "unknown";

    public string? UserAgent { get; set; }
}
