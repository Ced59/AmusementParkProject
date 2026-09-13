using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Core.Domain.Users;
using AmusementPark.Application.Ports;
using AmusementPark.Infrastructure.Services.Email;

namespace AmusementPark.Infrastructure.Services.Authentication;

internal sealed record LocalAccountEmailCopy(
    string Subject,
    string Badge,
    string Title,
    string Preheader,
    IReadOnlyCollection<string> Paragraphs,
    string ActionLabel,
    string FooterNote);
