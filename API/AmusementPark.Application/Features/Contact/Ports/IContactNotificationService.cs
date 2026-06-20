using AmusementPark.Core.Domain.Contact;

namespace AmusementPark.Application.Features.Contact.Ports;

public interface IContactNotificationService
{
    Task NotifySubmittedAsync(ContactGrievance grievance, CancellationToken cancellationToken);
}
