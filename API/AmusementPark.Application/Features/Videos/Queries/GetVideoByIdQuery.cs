using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Core.Domain.Videos;

namespace AmusementPark.Application.Features.Videos.Queries;

public sealed record GetVideoByIdQuery(string VideoId) : IQuery<ApplicationResult<Video>>;
