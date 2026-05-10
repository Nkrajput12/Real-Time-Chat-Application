using Microsoft.AspNetCore.Mvc;
using ConnectHub.Presence.Services;

namespace ConnectHub.Presence.Controllers;

[ApiController]
[Route("api/presence")]
public class PresenceController : ControllerBase
{
    private readonly IPresenceService _presence;

    public PresenceController(IPresenceService presence) => _presence = presence;

    [HttpGet("online")]
    public async Task<IActionResult> GetOnlineUsers() => Ok(await _presence.GetOnlineUserIds());

    [HttpGet("check/{userId}")]
    public async Task<IActionResult> IsOnline(int userId) => Ok(await _presence.IsUserOnline(userId));

    [HttpGet("count")]
    public async Task<IActionResult> GetTotalConnections() => Ok(await _presence.GetConnectionCount());
}