using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.SocialPublishing.Commands;
using AmusementPark.Application.Features.SocialPublishing.Contracts;
using AmusementPark.Application.Features.SocialPublishing.Ports;
using AmusementPark.Application.Features.SocialPublishing.Queries;
using AmusementPark.Core.Domain.SocialPublishing;

namespace AmusementPark.Application.Features.SocialPublishing.Handlers;

public sealed class GetSocialPublicationDraftQueryHandler
    : IQueryHandler<GetSocialPublicationDraftQuery, ApplicationResult<SocialPublicationDraft>>
{
    private readonly ISocialPublicationComposerService service;

    public GetSocialPublicationDraftQueryHandler(ISocialPublicationComposerService service)
    {
        this.service = service;
    }

    public Task<ApplicationResult<SocialPublicationDraft>> HandleAsync(
        GetSocialPublicationDraftQuery query,
        CancellationToken cancellationToken = default)
    {
        return this.service.ResolveDraftAsync(
            query.Url,
            query.ImagePage,
            query.ImagePageSize,
            cancellationToken);
    }
}

