using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Queries;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Application.Features.Sharing.Services;

namespace AmusementPark.Application.Features.Sharing.Handlers;

public sealed class GetProfileComparisonInvitationPreviewQueryHandler
    : IQueryHandler<GetProfileComparisonInvitationPreviewQuery,
        ApplicationResult<ProfileComparisonInvitationPreviewResult>>
{
    private readonly ProfileComparisonInvitationService service;

    public GetProfileComparisonInvitationPreviewQueryHandler(
        ProfileComparisonInvitationService service)
    {
        this.service = service ?? throw new ArgumentNullException(nameof(service));
    }

    public Task<ApplicationResult<ProfileComparisonInvitationPreviewResult>> HandleAsync(
        GetProfileComparisonInvitationPreviewQuery query,
        CancellationToken cancellationToken = default)
    {
        return this.service.PreviewAsync(query.UserId, query.Token, cancellationToken);
    }
}
