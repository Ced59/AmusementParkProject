using Entities.Model.Users;

namespace Services.Interfaces.Images
{
    public interface IUserAvatarService
    {
        Task<string> ImportExternalAvatarAsync(
            string imageUrl,
            string userId,
            ExternalLoginProvider provider,
            CancellationToken cancellationToken = default);
    }
}
