namespace AmusementPark.Application.Features.Users.Contracts;

public sealed record DeleteAccountRequest(
    string ConfirmationEmail,
    string CurrentPassword);
