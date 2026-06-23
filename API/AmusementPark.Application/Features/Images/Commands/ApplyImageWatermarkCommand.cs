using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Core.Domain.Images;

namespace AmusementPark.Application.Features.Images.Commands;

public sealed record ApplyImageWatermarkCommand(string ImageId) : ICommand<ApplicationResult<Image>>;
