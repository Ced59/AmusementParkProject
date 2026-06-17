using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.SocialShare.Contracts;

namespace AmusementPark.Application.Features.SocialShare.Commands;

public sealed record CaptureSocialShareEventCommand(SocialShareEventCapture Capture)
    : ICommand<ApplicationResult<SocialShareEventCaptureResult>>;
