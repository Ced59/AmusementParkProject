using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Videos.Contracts;
using AmusementPark.Core.Domain.Videos;

namespace AmusementPark.Application.Features.Videos.Commands;

public sealed record UpdateVideoCommand(string VideoId, VideoWriteModel Video) : ICommand<ApplicationResult<Video>>;
