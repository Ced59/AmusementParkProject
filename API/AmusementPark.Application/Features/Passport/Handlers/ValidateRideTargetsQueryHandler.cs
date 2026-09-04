using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Application.Features.Passport.Queries;

namespace AmusementPark.Application.Features.Passport.Handlers;

public sealed class ValidateRideTargetsQueryHandler
    : IQueryHandler<ValidateRideTargetsQuery, ApplicationResult<bool>>
{
    public const int MaximumTargetCount = 100;

    private readonly IVisitTargetResolver targetResolver;

    public ValidateRideTargetsQueryHandler(IVisitTargetResolver targetResolver)
    {
        this.targetResolver = targetResolver;
    }

    public async Task<ApplicationResult<bool>> HandleAsync(
        ValidateRideTargetsQuery query,
        CancellationToken cancellationToken = default)
    {
        string userId = query.UserId?.Trim() ?? string.Empty;
        string parkId = query.ParkId?.Trim() ?? string.Empty;
        string[] parkItemIds = query.ParkItemIds?
            .Select(static value => value?.Trim() ?? string.Empty)
            .Distinct(StringComparer.Ordinal)
            .ToArray() ?? Array.Empty<string>();
        if (userId.Length == 0
            || parkId.Length == 0
            || parkItemIds.Length is < 1 or > MaximumTargetCount
            || parkItemIds.Any(static value => value.Length == 0))
        {
            return ApplicationResult<bool>.Failure(
                PassportApplicationErrors.InvalidRideOccurrenceBatch());
        }

        IReadOnlyDictionary<string, VisitTarget> targets =
            await this.targetResolver.ResolveAsync(parkItemIds, cancellationToken);
        foreach (string parkItemId in parkItemIds)
        {
            ApplicationError? targetError =
                PassportRideOccurrenceHandlerSupport.ValidateTargetIdentity(
                    parkId,
                    parkItemId,
                    targets,
                    out VisitTarget? _);
            if (targetError is not null)
            {
                return ApplicationResult<bool>.Failure(targetError);
            }
        }

        return ApplicationResult<bool>.Success(true);
    }
}
