using Dtos.Users.Login;
using Entities.Model.Errors;
using OneOf;

namespace Services.Interfaces
{
    public interface IExternalAuthenticationService
    {
        Task<OneOf<UserLoggedDto, ErrorCodes.ErrorDetail>> AuthenticateAsync(
            string provider,
            string token,
            string? nonce,
            CancellationToken cancellationToken = default);
    }
}
