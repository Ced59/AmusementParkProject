using AmusementPark.Application.Features.Contact.Ports;
using AmusementPark.Core.Domain.Contact;

namespace AmusementPark.Application.Features.Contact.Services;

public sealed class NoOpContactNotificationService : IContactNotificationService
{
    public Task NotifySubmittedAsync(ContactGrievance grievance, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
