using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Contact.Contracts;
using AmusementPark.Core.Domain.Contact;

namespace AmusementPark.Application.Features.Contact.Queries;

public sealed record GetContactGrievancesQuery(PagedQuery Pagination, ContactGrievanceSearchCriteria Criteria)
    : IQuery<ApplicationResult<PagedResult<ContactGrievance>>>;
