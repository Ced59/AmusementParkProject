using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Core.Domain.Images;

namespace AmusementPark.Application.Features.Images.Commands;

public sealed record ImportRemoteImageCommand(RemoteImageImportRequest Request) : ICommand<ApplicationResult<Image>>;
