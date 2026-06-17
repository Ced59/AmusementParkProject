using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.Contact.Contracts;
using AmusementPark.Core.Domain.Contact;

namespace AmusementPark.Application.Features.Contact.Ports;

public interface IContactGrievanceRepository
{
    Task<ContactGrievance> CreateAsync(ContactGrievance grievance, CancellationToken cancellationToken);

    Task<long> CountRecentByIpAsync(string ipAddress, DateTime submittedSinceUtc, CancellationToken cancellationToken);

    Task<PagedResult<ContactGrievance>> GetPageAsync(int page, int pageSize, ContactGrievanceSearchCriteria criteria, CancellationToken cancellationToken);
}
