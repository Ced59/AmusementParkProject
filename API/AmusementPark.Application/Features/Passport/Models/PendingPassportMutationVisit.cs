using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Models;

public sealed record PendingPassportMutationVisit(
    string UserId,
    VisitId VisitId,
    string OperationKeyHash,
    PendingPassportMutationKind Kind,
    RideOccurrenceCreationPreparation? CreationPreparation,
    long? ContentFenceToken = null);
