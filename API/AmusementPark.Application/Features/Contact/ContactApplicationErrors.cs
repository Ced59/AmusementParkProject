using AmusementPark.Application.Errors;

namespace AmusementPark.Application.Features.Contact;

public static class ContactApplicationErrors
{
    public static ApplicationError InvalidSubmission(IReadOnlyDictionary<string, IReadOnlyCollection<string>> details)
    {
        return ApplicationError.Validation("contact.grievance.invalid", "Le message de contact est invalide.", details);
    }

    public static ApplicationError TooManySubmissions()
    {
        return ApplicationError.RuleViolation("contact.grievance.too-many-submissions", "Trop de messages ont ete envoyes depuis cette adresse. Reessayez plus tard.");
    }
}
