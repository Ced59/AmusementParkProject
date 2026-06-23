using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.TechnicalStats.Contracts;

namespace AmusementPark.Application.Features.TechnicalStats.Commands;

public sealed record UpdateTechnicalStatsSettingsCommand(TechnicalStatsSettings Settings)
    : ICommand<ApplicationResult<TechnicalStatsSettings>>;
