using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.StandaloneAttractions.Contracts;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.StandaloneAttractions.Commands;

public sealed record CreateStandaloneAttractionCommand(StandaloneAttraction Attraction)
    : ICommand<ApplicationResult<StandaloneAttraction>>;

public sealed record UpdateStandaloneAttractionCommand(string Id, StandaloneAttraction Attraction)
    : ICommand<ApplicationResult<StandaloneAttraction>>;

public sealed record UpdateStandaloneAttractionsBulkAdministrationCommand(
    IReadOnlyCollection<string> Ids,
    bool? IsVisible,
    AdminReviewStatus? AdminReviewStatus)
    : ICommand<ApplicationResult<BulkAdministrationUpdateResult>>;

public sealed record MigrateParkToStandaloneAttractionCommand(StandaloneAttractionMigrationRequest Request)
    : ICommand<ApplicationResult<StandaloneAttraction>>;

