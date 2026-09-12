using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Passport.Commands;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Application.Features.Passport.Queries;
using AmusementPark.Application.Features.Passport.Results;
using AmusementPark.Application.Features.Passport.Services;
using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Handlers;

public sealed class ListUserVisitsQueryHandler : IQueryHandler<ListUserVisitsQuery, ApplicationResult<VisitPageResult>>
{
    private readonly IUserVisitRepository visitRepository;
    private readonly IParkNameReadRepository parkNameReadRepository;

    public ListUserVisitsQueryHandler(
        IUserVisitRepository visitRepository,
        IParkNameReadRepository parkNameReadRepository)
    {
        this.visitRepository = visitRepository;
        this.parkNameReadRepository = parkNameReadRepository;
    }

    public async Task<ApplicationResult<VisitPageResult>> HandleAsync(
        ListUserVisitsQuery query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.UserId))
        {
            return ApplicationResult<VisitPageResult>.Failure(
                ApplicationErrors.Required(nameof(query.UserId)));
        }

        if (query.Limit is < 1 or > UserVisitListCriteria.MaximumLimit)
        {
            return ApplicationResult<VisitPageResult>.Failure(
                PassportApplicationErrors.InvalidListLimit());
        }

        if (query.Year.HasValue
            && query.Year.Value is < 1 or > 9999)
        {
            return ApplicationResult<VisitPageResult>.Failure(
                PassportApplicationErrors.InvalidYear());
        }

        if (query.Status.HasValue && !Enum.IsDefined(query.Status.Value))
        {
            return ApplicationResult<VisitPageResult>.Failure(
                PassportApplicationErrors.InvalidStatus());
        }

        UserVisitPage page = await this.visitRepository.ListOwnedAsync(
            new UserVisitListCriteria(
                query.UserId.Trim(),
                query.Limit,
                string.IsNullOrWhiteSpace(query.ParkId) ? null : query.ParkId.Trim(),
                query.Year,
                query.Status,
                query.After),
            cancellationToken);
        string[] parkIds = page.Items
            .Select(static visit => visit.ParkId)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        IReadOnlyDictionary<string, string?> parkNames = parkIds.Length == 0
            ? new Dictionary<string, string?>(StringComparer.Ordinal)
            : await this.parkNameReadRepository.GetNamesByIdsAsync(parkIds, cancellationToken);
        VisitPageResult result = new VisitPageResult(
            page.Items
                .Select(visit => PassportVisitResultFactory.Create(
                    visit,
                    parkNames.GetValueOrDefault(visit.ParkId)))
                .ToList(),
            page.NextCursor);
        return ApplicationResult<VisitPageResult>.Success(result);
    }
}
