using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using ConnectHub.Messages.Services.Interface;
using ConnectHub.MessageService.Models;

namespace ConnectHub.Messages.Controllers;

[ApiController]
[Route("api/messages")]
[Authorize] 
public class MessageController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;
    private readonly IMessageService _messageService;

    public MessageController(IMessageService messageService, IHttpClientFactory httpClientFactory, IConfiguration config)
    {
        _messageService = messageService;
        _httpClientFactory = httpClientFactory;
        _config = config;
    }
    
    [HttpGet("direct/{receiverId}")]
    public async Task<IActionResult> GetDirect(int receiverId)
    {
        var userId = GetCurrentUserId();
        var messages = await _messageService.GetDirectMessagesAsync(userId, receiverId);
        return Ok(messages);
    }

    [HttpGet("room/{roomId}")]
    public async Task<IActionResult> GetRoom(int roomId)
    {
        var messages = await _messageService.GetRoomMessagesAsync(roomId);
        return Ok(messages);
    }

    [HttpGet("unread")]
    public async Task<IActionResult> GetUnread()
    {
        var userId = GetCurrentUserId();
        var messages = await _messageService.GetUnreadMessagesAsync(userId);
        return Ok(messages);
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userId = GetCurrentUserId();
        var count = await _messageService.GetUnreadCountAsync(userId);
        return Ok(new { count });
    }

    [HttpGet("recent")]
    public async Task<IActionResult> GetRecent()
    {
        var userId = GetCurrentUserId();
        var chats = await _messageService.GetRecentChatsAsync(userId);
        return Ok(chats);
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string query)
    {
        var userId = GetCurrentUserId();
        var results = await _messageService.SearchMessagesAsync(query, userId);
        return Ok(results);
    }



    [HttpPut("markRead/{id}")]
    public async Task<IActionResult> MarkRead(int id)
    {
        await _messageService.MarkAsReadAsync(id);
        return Ok(new { message = "Marked as read" });
    }

    [HttpPut("mark-all-read/{senderId}")]
    public async Task<IActionResult> MarkAllRead(int senderId)
    {
        var userId = GetCurrentUserId();
        await _messageService.MarkAllAsReadAsync(userId, null); // null for direct messages
        return Ok(new { message = "All messages marked as read" });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Edit(int id, [FromBody] ConnectHub.MessageService.Models.DTOs.UpdateMessageDto dto)
    {
        var success = await _messageService.EditMessageAsync(id, dto.Content);
        if (!success) return NotFound();
        return Ok(new { message = "Message edited" });
    }


    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var message = await _messageService.GetMessageByIdAsync(id);
        if (message == null) return NotFound();

        var currentUserId = GetCurrentUserId();
        bool authorized = message.SenderId == currentUserId;

        if (!authorized && message.RoomId.HasValue)
        {
            // Check if user is admin/moderator of the room
            var client = _httpClientFactory.CreateClient();
            // Assuming ChatRoom service is at http://localhost:5002 as per my previous setup
            // Or use service discovery if available. 
            var roomServiceUrl = _config["Services:ChatRoomUrl"] ?? "http://localhost:5002";
            var response = await client.GetAsync($"{roomServiceUrl}/api/rooms/{message.RoomId}/is-admin/{currentUserId}");
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<AdminCheckResponse>();
                authorized = result?.IsAdmin ?? false;
            }
        }

        if (!authorized) return Forbid();

        var success = await _messageService.DeleteMessageAsync(id);
        return success ? Ok(new { message = "Message deleted" }) : BadRequest();
    }

    private class AdminCheckResponse { public bool IsAdmin { get; set; } }

    // Helper to extract NameIdentifier from JWT
    private int GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return claim != null ? int.Parse(claim.Value) : 0;
    }
}


