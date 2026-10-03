using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Users.Contracts;

namespace AmusementPark.Application.Features.Users.Commands;

public sealed record DeleteAccountCommand(
    string UserId,
    DeleteAccountRequest Request) : ICommand<ApplicationResult>;
