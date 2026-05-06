using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ConnectHub.Rooms.Models;
using ConnectHub.Rooms.Services.Interface;

namespace ConnectHub.Rooms.Controllers;

[ApiController]
[Route("api/admin/rooms")]
[Authorize(Roles = "Admin")] //System Admin
public class AdminRoomController : ControllerBase
{
    private readonly IChatRoomService _roomService;

    public AdminRoomController(IChatRoomService roomService)
    {
        _roomService = roomService;
    }

    [HttpGet("all-stats")]
    public async Task<IActionResult> GetAllRoomStats()
    {
        return Ok(await _roomService.GetPublicRoomsAsync());
    }

    [HttpDelete("force-delete/{roomId}")]
    public async Task<IActionResult> ForceDelete(int roomId)
    {
        var success = await _roomService.DeleteRoomAsync(roomId);
        return success ? Ok("Room id deleted by System Admin") : NotFound();
    }
}