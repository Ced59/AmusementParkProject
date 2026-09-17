namespace AmusementPark.Application.Features.Watchlists.Ports;

public interface INotificationEmailUnsubscribeTokenProtector
{
    string CreateToken(string userId);

    bool TryReadUserId(string token, out string userId);
}
