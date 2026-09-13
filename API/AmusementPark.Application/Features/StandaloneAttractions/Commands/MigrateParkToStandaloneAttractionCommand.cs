using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.StandaloneAttractions.Contracts;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.StandaloneAttractions.Commands;

public sealed record MigrateParkToStandaloneAttractionCommand(StandaloneAttractionMigrationRequest Request)
    : ICommand<ApplicationResult<StandaloneAttraction>>;
