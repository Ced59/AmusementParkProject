using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;

namespace AmusementPark.Application.Features.Videos.Commands;

public sealed record DeleteVideoCommand(string VideoId) : ICommand<ApplicationResult>;
