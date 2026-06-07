using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkItems.Contracts;
using AmusementPark.Application.Features.ParkItems.Results;

namespace AmusementPark.Application.Features.ParkItems.Commands;

public sealed record PreviewParkItemsBulkCreateCommand(
    string ParkId,
    IReadOnlyCollection<ParkItemBulkCreateDraft> Rows)
    : ICommand<ApplicationResult<ParkItemsBulkCreatePreviewResult>>;
