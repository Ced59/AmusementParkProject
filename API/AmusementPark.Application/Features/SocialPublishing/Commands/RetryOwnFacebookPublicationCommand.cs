using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Core.Domain.SocialPublishing;

namespace AmusementPark.Application.Features.SocialPublishing.Commands;

public sealed record RetryOwnFacebookPublicationCommand(
    string PublicationId,
    string? RequestedByUserId) : ICommand<ApplicationResult<SocialPublication>>;
