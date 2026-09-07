using AmusementPark.Application.Features.Sharing.Models;

namespace AmusementPark.Application.Features.Sharing.Results;

public sealed record SharePublicationCommitResult(
    SharePublicationSettingsResult Settings,
    SharePublicationApprovalState PublicationState,
    bool WasWritten);
