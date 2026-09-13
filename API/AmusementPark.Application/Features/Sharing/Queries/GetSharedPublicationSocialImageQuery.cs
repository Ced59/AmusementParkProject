using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Queries;

public sealed record GetSharedPublicationSocialImageQuery(
    string ShareId,
    SharePublicationType PublicationType,
    long PublicationVersion,
    string Language) : IQuery<ApplicationResult<ShareSocialImageRenderResult>>;
