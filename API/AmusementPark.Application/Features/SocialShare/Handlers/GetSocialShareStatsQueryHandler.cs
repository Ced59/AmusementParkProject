using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.SocialShare.Contracts;
using AmusementPark.Application.Features.SocialShare.Ports;
using AmusementPark.Application.Features.SocialShare.Queries;

namespace AmusementPark.Application.Features.SocialShare.Handlers;

public sealed class GetSocialShareStatsQueryHandler
    : IQueryHandler<GetSocialShareStatsQuery, ApplicationResult<SocialShareStatsResult>>
{
    private static readonly TimeSpan DefaultRange = TimeSpan.FromDays(30);
    private static readonly TimeSpan MaximumRange = TimeSpan.FromDays(180);

    private readonly ISocialShareEventRepository repository;

    public GetSocialShareStatsQueryHandler(ISocialShareEventRepository repository)
    {
        this.repository = repository;
    }

    public async Task<ApplicationResult<SocialShareStatsResult>> HandleAsync(GetSocialShareStatsQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        DateTime nowUtc = DateTime.UtcNow;
        DateTime toUtc = NormalizeDate(query.Criteria.ToUtc) ?? nowUtc;
        DateTime fromUtc = NormalizeDate(query.Criteria.FromUtc) ?? toUtc.Subtract(DefaultRange);

        if (fromUtc > toUtc)
        {
            return ApplicationResult<SocialShareStatsResult>.Failure(SocialShareApplicationErrors.InvalidDateRange());
        }

        if (toUtc.Subtract(fromUtc) > MaximumRange)
        {
            fromUtc = toUtc.Subtract(MaximumRange);
        }

        SocialShareStatsResult result = await this.repository.GetStatsAsync(fromUtc, toUtc, cancellationToken);
        return ApplicationResult<SocialShareStatsResult>.Success(result);
    }

    private static DateTime? NormalizeDate(DateTime? value)
    {
        if (!value.HasValue)
        {
            return null;
        }

        return value.Value.Kind == DateTimeKind.Utc ? value.Value : value.Value.ToUniversalTime();
    }
}
