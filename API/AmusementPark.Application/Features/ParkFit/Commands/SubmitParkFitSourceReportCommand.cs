using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Core.Domain.ParkFit;

namespace AmusementPark.Application.Features.ParkFit.Commands;

public sealed record SubmitParkFitSourceReportCommand(
    string ParkId,
    ParkFitEvidenceKind EvidenceKind,
    string? SourceUrl,
    string? SourceReference,
    ParkFitSourceReportReason Reason,
    string? Details) : ICommand<ApplicationResult>;
