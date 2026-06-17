using AmusementPark.Application.Features.Contact.Contracts;
using AmusementPark.Core.Domain.Contact;
using AmusementPark.WebAPI.Contracts.Contact;

namespace AmusementPark.WebAPI.Mappers;

public static class ContactHttpMappers
{
    public static ContactGrievanceSubmission ToApplication(this SubmitContactGrievanceRequest request, string ipAddress, string? userAgent)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new ContactGrievanceSubmission(
            request.Message,
            request.Website,
            request.LanguageCode,
            ipAddress,
            userAgent);
    }

    public static ContactGrievanceSubmissionDto ToHttp(this ContactGrievanceSubmissionResult result)
    {
        return new ContactGrievanceSubmissionDto
        {
            Accepted = result.Accepted,
            SubmittedAtUtc = result.SubmittedAtUtc,
        };
    }

    public static AdminContactGrievanceDto ToAdminHttp(this ContactGrievance grievance)
    {
        return new AdminContactGrievanceDto
        {
            Id = grievance.Id ?? string.Empty,
            Message = grievance.Message,
            LanguageCode = grievance.LanguageCode,
            IpAddress = grievance.IpAddress,
            UserAgent = grievance.UserAgent,
            CreatedAtUtc = grievance.CreatedAtUtc,
        };
    }
}
