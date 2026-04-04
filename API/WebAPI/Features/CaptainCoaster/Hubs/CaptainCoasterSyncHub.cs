using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace WebAPI.Features.CaptainCoaster.Hubs
{
    [Authorize(Roles = "ADMIN")]
    public sealed class CaptainCoasterSyncHub : Hub
    {
        public const string HubPath = "/hubs/captain-coaster-sync";
    }
}
