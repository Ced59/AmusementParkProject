using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.SocialPublishing.Contracts;
using AmusementPark.Core.Domain.SocialPublishing;

namespace AmusementPark.Application.Features.SocialPublishing.Commands;

public sealed record RefreshParkAnnouncementPreviewCommand(
    string ParkId,
    string? RequestedByUserId) : ICommand<ApplicationResult<SocialPublication>>;

