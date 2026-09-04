using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Passport.Models;

namespace AmusementPark.Application.Features.Passport.Commands;

public sealed record DeleteVisitCommand(
    string UserId,
    string VisitId,
    long ExpectedVersion,
    long ConfirmedOccurrenceCount,
    long ConfirmedAssessmentCount,
    string ClientOperationId) : ICommand<ApplicationResult<VisitDeletionReceipt>>;
