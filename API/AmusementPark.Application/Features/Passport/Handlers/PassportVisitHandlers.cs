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

public sealed class CreateVisitCommandHandler : ICommandHandler<CreateVisitCommand, ApplicationResult<CreateVisitResult>>
{
    public const int MaximumClientOperationIdLength = 128;

    private readonly IUserVisitRepository visitRepository;
    private readonly IParkRepository parkRepository;
    private readonly IPassportClock clock;
    private readonly IPassportTimeZoneValidator timeZoneValidator;
    private readonly IPassportAuditPublisher? auditPublisher;

    internal CreateVisitCommandHandler(
        IUserVisitRepository visitRepository,
        IParkRepository parkRepository,
        IPassportClock clock,
        IPassportTimeZoneValidator timeZoneValidator)
        : this(visitRepository, parkRepository, clock, timeZoneValidator, null!)
    {
    }

    public CreateVisitCommandHandler(
        IUserVisitRepository visitRepository,
        IParkRepository parkRepository,
        IPassportClock clock,
        IPassportTimeZoneValidator timeZoneValidator,
        IPassportAuditPublisher auditPublisher)
    {
        this.visitRepository = visitRepository;
        this.parkRepository = parkRepository;
        this.clock = clock;
        this.timeZoneValidator = timeZoneValidator;
        this.auditPublisher = auditPublisher;
    }

    public async Task<ApplicationResult<CreateVisitResult>> HandleAsync(
        CreateVisitCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.UserId))
        {
            return ApplicationResult<CreateVisitResult>.Failure(
                ApplicationErrors.Required(nameof(command.UserId)));
        }

        if (string.IsNullOrWhiteSpace(command.ParkId))
        {
            return ApplicationResult<CreateVisitResult>.Failure(
                ApplicationErrors.Required(nameof(command.ParkId)));
        }

        string? clientOperationId = NormalizeClientOperationId(command.ClientOperationId);
        if (clientOperationId is null)
        {
            return ApplicationResult<CreateVisitResult>.Failure(
                PassportApplicationErrors.InvalidIdempotencyKey());
        }

        VisitDate date;
        try
        {
            date = new VisitDate(
                command.Year,
                command.Month,
                command.Day,
                command.Precision,
                command.IsApproximate);
        }
        catch (VisitDateValidationException exception)
        {
            return ApplicationResult<CreateVisitResult>.Failure(
                PassportApplicationErrors.InvalidDate(
                    exception.ErrorCode,
                    exception.Message,
                    exception.ParamName));
        }

        string? timeZoneId = string.IsNullOrWhiteSpace(command.TimeZoneId)
            ? null
            : command.TimeZoneId.Trim();
        string userId = command.UserId.Trim();
        string parkId = command.ParkId.Trim();
        Visit visit;
        try
        {
            visit = Visit.Create(
                VisitId.New(),
                userId,
                parkId,
                date,
                timeZoneId,
                command.ServiceDayConvention,
                command.Title,
                command.PrivateNote,
                this.clock.UtcNow);
        }
        catch (VisitValidationException exception)
        {
            return ApplicationResult<CreateVisitResult>.Failure(
                PassportApplicationErrors.InvalidVisit(
                    exception.ErrorCode,
                    exception.Message));
        }
        catch (IdentifierValidationException exception)
        {
            return ApplicationResult<CreateVisitResult>.Failure(
                PassportApplicationErrors.InvalidIdentifier(
                    exception.ErrorCode,
                    exception.Message,
                    exception.ParamName));
        }

        IdempotentVisitCreationResult? existingCreation =
            await this.visitRepository.ResolveExistingCreationAsync(
                visit,
                clientOperationId,
                cancellationToken);
        if (existingCreation is not null)
        {
            return ToApplicationResult(existingCreation);
        }

        if (timeZoneId is not null && !this.timeZoneValidator.IsValid(timeZoneId))
        {
            return ApplicationResult<CreateVisitResult>.Failure(
                PassportApplicationErrors.InvalidTimeZone());
        }

        Park? park = await this.parkRepository.GetByIdAsync(
            parkId,
            includeHidden: true,
            cancellationToken);
        if (park is null)
        {
            return ApplicationResult<CreateVisitResult>.Failure(
                PassportApplicationErrors.ParkNotFound());
        }

        PassportAuditEvent? auditEvent = this.auditPublisher is null
            ? null
            : PassportVisitAuditEventFactory.VisitCreated(visit, clientOperationId);
        IdempotentVisitCreationResult creation = auditEvent is null
            ? await this.visitRepository.CreateIdempotentAsync(
                visit,
                clientOperationId,
                cancellationToken)
            : await this.visitRepository.CreateIdempotentAuditedAsync(
                visit,
                clientOperationId,
                auditEvent,
                cancellationToken);
        if (creation.Visit is not null
            && creation.Status != IdempotentVisitCreationStatus.Conflict)
        {
            PassportAuditEvent? persistedEvent = this.auditPublisher is null
                ? null
                : PassportVisitAuditEventFactory.VisitCreated(
                    creation.Visit,
                    clientOperationId);
            await PassportAuditDelivery.PublishAsync(
                this.auditPublisher,
                persistedEvent,
                cancellationToken);
        }

        return ToApplicationResult(creation);
    }

    private static ApplicationResult<CreateVisitResult> ToApplicationResult(
        IdempotentVisitCreationResult creation)
    {
        if (creation.Status == IdempotentVisitCreationStatus.Conflict
            || creation.Visit is null)
        {
            return ApplicationResult<CreateVisitResult>.Failure(
                PassportApplicationErrors.IdempotencyConflict());
        }

        CreateVisitResult result = new CreateVisitResult(
            PassportVisitResultFactory.Create(creation.Visit),
            creation.Status == IdempotentVisitCreationStatus.Replayed);
        return ApplicationResult<CreateVisitResult>.Success(result);
    }

    private static string? NormalizeClientOperationId(string? value)
    {
        string normalizedValue = value?.Trim() ?? string.Empty;
        if (normalizedValue.Length is < 1 or > MaximumClientOperationIdLength)
        {
            return null;
        }

        foreach (char character in normalizedValue)
        {
            if (char.IsControl(character))
            {
                return null;
            }
        }

        return normalizedValue;
    }
}

public sealed class GetVisitQueryHandler : IQueryHandler<GetVisitQuery, ApplicationResult<VisitResult>>
{
    private readonly IUserVisitRepository visitRepository;

    public GetVisitQueryHandler(IUserVisitRepository visitRepository)
    {
        this.visitRepository = visitRepository;
    }

    public async Task<ApplicationResult<VisitResult>> HandleAsync(
        GetVisitQuery query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.UserId))
        {
            return ApplicationResult<VisitResult>.Failure(
                ApplicationErrors.Required(nameof(query.UserId)));
        }

        VisitId visitId;
        try
        {
            visitId = VisitId.Parse(query.VisitId);
        }
        catch (ArgumentException)
        {
            return ApplicationResult<VisitResult>.Failure(
                PassportApplicationErrors.VisitNotFound());
        }

        Visit? visit = await this.visitRepository.GetOwnedAsync(
            visitId,
            query.UserId.Trim(),
            cancellationToken);
        return visit is null
            ? ApplicationResult<VisitResult>.Failure(PassportApplicationErrors.VisitNotFound())
            : ApplicationResult<VisitResult>.Success(PassportVisitResultFactory.Create(visit));
    }
}

public sealed class ListUserVisitsQueryHandler : IQueryHandler<ListUserVisitsQuery, ApplicationResult<VisitPageResult>>
{
    private readonly IUserVisitRepository visitRepository;
    private readonly IParkRepository parkRepository;

    public ListUserVisitsQueryHandler(
        IUserVisitRepository visitRepository,
        IParkRepository parkRepository)
    {
        this.visitRepository = visitRepository;
        this.parkRepository = parkRepository;
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
        IReadOnlyCollection<Park> parks = parkIds.Length == 0
            ? Array.Empty<Park>()
            : await this.parkRepository.GetByIdsAsync(parkIds, cancellationToken);
        IReadOnlyDictionary<string, string?> parkNames = parks
            .Where(static park => !string.IsNullOrWhiteSpace(park.Id))
            .GroupBy(static park => park.Id!, StringComparer.Ordinal)
            .ToDictionary(
                static group => group.Key,
                static group => group.First().Name,
                StringComparer.Ordinal);
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
