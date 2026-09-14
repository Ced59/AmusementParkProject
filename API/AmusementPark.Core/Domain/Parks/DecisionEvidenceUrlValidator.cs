namespace AmusementPark.Core.Domain.Parks;

/// <summary>
/// Valide les URL de preuve consultables utilisées par les décisions FIT.
/// </summary>
internal static class DecisionEvidenceUrlValidator
{
    public static bool IsValid(string? value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out Uri? uri)
            && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);
    }
}
