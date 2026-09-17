namespace AmusementPark.Application.Ports;

public sealed record EmailMessage(
    string To,
    string Subject,
    string HtmlBody,
    string? TextBody = null,
    IReadOnlyDictionary<string, string>? Headers = null);
