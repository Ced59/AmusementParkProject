using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Queries;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Application.Features.Sharing.Services;

namespace AmusementPark.Application.Features.Sharing.Handlers;

public sealed class ListMyProfileComparisonsQueryHandler
    : IQueryHandler<ListMyProfileComparisonsQuery,
        ApplicationResult<IReadOnlyCollection<ProfileComparisonSummaryResult>>>
{
    private readonly ProfileComparisonReader reader;

    public ListMyProfileComparisonsQueryHandler(ProfileComparisonReader reader)
    {
        this.reader = reader ?? throw new ArgumentNullException(nameof(reader));
    }

    public Task<ApplicationResult<IReadOnlyCollection<ProfileComparisonSummaryResult>>>
        HandleAsync(
            ListMyProfileComparisonsQuery query,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        return this.reader.ListForParticipantAsync(query.UserId, cancellationToken);
    }
}
