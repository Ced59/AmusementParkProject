using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Contact.Ports;
using AmusementPark.Application.Features.Contact.Queries;
using AmusementPark.Core.Domain.Contact;

namespace AmusementPark.Application.Features.Contact.Handlers;

public sealed class GetContactGrievancesQueryHandler
    : IQueryHandler<GetContactGrievancesQuery, ApplicationResult<PagedResult<ContactGrievance>>>
{
    private readonly IContactGrievanceRepository repository;

    public GetContactGrievancesQueryHandler(IContactGrievanceRepository repository)
    {
        this.repository = repository;
    }

    public async Task<ApplicationResult<PagedResult<ContactGrievance>>> HandleAsync(GetContactGrievancesQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.Pagination.Page <= 0 || query.Pagination.PageSize <= 0 || query.Pagination.PageSize > 100)
        {
            return ApplicationResult<PagedResult<ContactGrievance>>.Failure(ApplicationErrors.InvalidPagination());
        }

        PagedResult<ContactGrievance> page = await this.repository.GetPageAsync(query.Pagination.Page, query.Pagination.PageSize, query.Criteria, cancellationToken);
        return ApplicationResult<PagedResult<ContactGrievance>>.Success(page);
    }
}
