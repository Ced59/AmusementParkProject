namespace AmusementPark.WebAPI.Contracts.Contact;

public sealed class SubmitContactGrievanceRequest
{
    public string? Message { get; set; }

    public string? Website { get; set; }

    public string? LanguageCode { get; set; }
}
