using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using ConnectHub.Rooms.Models;
using ConnectHub.Rooms.Services.Interface;
using ConnectHub.Rooms.DTOs;

namespace ConnectHub.Rooms.Controllers;

[ApiController]
[Route("api/rooms")]
[Authorize] 
public class ChatRoomController : ControllerBase
{
    private readonly IChatRoomService _roomService;

    public ChatRoomController(IChatRoomService roomService)
    {
        _roomService = roomService;
    }

    [HttpPost("Create")]
    public async Task<IActionResult> CreateRoom([FromBody] CreateRoomDto roomDto)
    {
        var room = new ChatRoom{
            RoomName = roomDto.RoomName,
            Description = roomDto.Description,
            RoomType = roomDto.RoomType,
            AvatarUrl = roomDto.AvatarUrl,
            MaxMembers = roomDto.MaxMembers,
            CreatedBy = GetUserId()
        };

        var result = await _roomService.CreateRoomAsync(room);
        return Ok(new {id = result.RoomId});
    }

    [HttpPost("{roomId}/addMember/{targetUserId}")]
    public async Task<IActionResult> AddMember(int roomId, int targetUserId)
    {
        if(!await IsModerator(roomId)) return Forbid();

        var success = await _roomService.AddMemberAsync(roomId, targetUserId);
        return success ? Ok("Member added successfully.") : BadRequest("Could not add member.");
    }

    [HttpGet("GetByID/{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var room = await _roomService.GetRoomByIdAsync(id);
        return room == null ? NotFound() : Ok(room);
    }

    [HttpGet("my-rooms")]
    public async Task<IActionResult> GetMyRooms()
    {
        return Ok(await _roomService.GetRoomsByUserAsync(GetUserId()));
    }

    [HttpGet("public")]
    public async Task<IActionResult> GetPublicRooms()
    {
        return Ok(await _roomService.GetPublicRoomsAsync());
    }

    [HttpGet("Getallmembers/{roomId}")]
    public async Task<IActionResult> GetMembers(int roomId)
    {

        if (!await _roomService.IsUserInRoomAsync(roomId, GetUserId())) return Forbid();
        
        return Ok(await _roomService.GetMembersAsync(roomId));
    }

    [HttpPut("UpdateRoom/{id}")]
    public async Task<IActionResult> UpdateRoom(int id, [FromBody] CreateRoomDto roomDto)
    {
        if (!await IsAdmin(id)) return Forbid();

        var updatedRoom = new ChatRoom
        {
            RoomName = roomDto.RoomName,
            Description = roomDto.Description,
            RoomType = roomDto.RoomType,
            AvatarUrl = roomDto.AvatarUrl,
            MaxMembers = roomDto.MaxMembers
        };

        var success = await _roomService.UpdateRoomAsync(id, updatedRoom);
        return success ? Ok("Room updated.") : NotFound();
    }

    [HttpPut("{roomId}/members/{targetUserId}/role")]
    public async Task<IActionResult> UpdateMemberRole(int roomId, int targetUserId, [FromBody] MemberRole newRole)
    {
        if (!await IsAdmin(roomId)) return Forbid();

        var success = await _roomService.UpdateMemberRoleAsync(roomId, targetUserId, newRole);
        return success ? Ok("Role updated.") : NotFound();
    }

    [HttpDelete("DeleteRoom/{id}")]
    public async Task<IActionResult> DeleteRoom(int id)
    {
        if (!await IsAdmin(id)) return Forbid();

        var success = await _roomService.DeleteRoomAsync(id);
        return success ? Ok("Room deactivated.") : NotFound();
    }

    [HttpDelete("{roomId}/removeMember/{targetUserId}")]
    public async Task<IActionResult> RemoveMember(int roomId, int targetUserId)
    {
        if (!await IsModerator(roomId)) return Forbid();

        var success = await _roomService.RemoveMemberAsync(roomId, targetUserId);
        return success ? Ok("Member removed.") : NotFound();
    }

    [HttpGet("{roomId}/is-admin/{userId}")]
    [AllowAnonymous] // Or internal only if possible, but for simplicity
    public async Task<IActionResult> CheckIsAdmin(int roomId, int userId)
    {
        var members = await _roomService.GetMembersAsync(roomId);
        var member = members.FirstOrDefault(m => m.UserId == userId);
        var isAdmin = member != null && (member.Role == MemberRole.ADMIN || member.Role == MemberRole.MODERATOR);
        return Ok(new { isAdmin });
    }

    [HttpDelete("leave/{roomId}")]
    public async Task<IActionResult> LeaveRoom(int roomId)
    {
        var success = await _roomService.LeaveRoomAsync(roomId, GetUserId());
        return success ? Ok("Left room.") : BadRequest();
    }

    private int GetUserId()
    {
        return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }

    private async Task<bool> IsAdmin(int roomId)
    {
        var members = await _roomService.GetMembersAsync(roomId);
        var me = members.FirstOrDefault(m => m.UserId == GetUserId());

        return me != null && me.Role == MemberRole.ADMIN;
    }

    private async Task<bool> IsModerator(int roomId)
    {
        var members = await _roomService.GetMembersAsync(roomId);
        var me = members.FirstOrDefault(m => m.UserId == GetUserId());
        return me != null && (me.Role == MemberRole.ADMIN || me.Role == MemberRole.MODERATOR);
    }
}