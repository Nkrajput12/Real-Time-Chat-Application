using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace ConnectHub.Presence.Providers; 

public class NameUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        return connection.User?.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}