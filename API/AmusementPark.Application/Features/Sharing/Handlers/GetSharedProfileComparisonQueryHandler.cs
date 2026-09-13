using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Queries;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Application.Features.Sharing.Services;

namespace AmusementPark.Application.Features.Sharing.Handlers;

public sealed class GetSharedProfileComparisonQueryHandler
    : IQueryHandler<GetSharedProfileComparisonQuery,
        ApplicationResult<SharedProfileComparisonResult>>
{
    private readonly ProfileComparisonReader reader;

    public GetSharedProfileComparisonQueryHandler(ProfileComparisonReader reader)
    {
        this.reader = reader ?? throw new ArgumentNullException(nameof(reader));
    }

    public Task<ApplicationResult<SharedProfileComparisonResult>> HandleAsync(
        GetSharedProfileComparisonQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        return this.reader.GetSharedAsync(query.ShareId, cancellationToken);
    }
}
