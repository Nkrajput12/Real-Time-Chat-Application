using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using ConnectHub.Presence.Services;
using MassTransit;
using ConnectHub.Shared.Events;

namespace ConnectHub.Presence.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly IPresenceService _presence;
    private readonly IPublishEndpoint _publishEndpoint;

    public ChatHub(IPresenceService presence, IPublishEndpoint publishEndpoint)
    {
        _presence = presence;
        _publishEndpoint = publishEndpoint;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = int.Parse(Context.UserIdentifier!);
        await _presence.UserConnected(userId, Context.ConnectionId);

        await Clients.Others.SendAsync("UserOnline", userId);
        
        // Initial state: Send list of currently online users to the caller
        var onlineUsers = await _presence.GetOnlineUserIds();
        await Clients.Caller.SendAsync("OnlineUsers", onlineUsers);
        
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = int.Parse(Context.UserIdentifier!);
        await _presence.UserDisconnected(userId, Context.ConnectionId);
        if (!await _presence.IsUserOnline(userId))
        {
            await Clients.Others.SendAsync("UserOffline", userId, DateTime.UtcNow.ToString());
        }

        await base.OnDisconnectedAsync(exception);
    }

    // Hub Method: Send Direct Message
    public async Task SendDirectMessage(int receiverId, string message, string? recipientEmail, string messageType = "TEXT", string? mediaUrl = null)
    {
        var senderId = int.Parse(Context.UserIdentifier!);
        
        var messageEvent = new MessageSentEvent
        {
            SenderId = senderId,
            ReceiverId = receiverId,
            Content = message,
            RecipientEmail = recipientEmail,
            SentAt = DateTime.UtcNow,
            MessageType = messageType,
            MediaUrl = mediaUrl
        };

        // Publish event for persistence
        await _publishEndpoint.Publish(messageEvent);
        
        var messageObj = new {
            SenderId = senderId,
            ReceiverId = receiverId,
            Content = message,
            Timestamp = messageEvent.SentAt,
            MessageType = messageType,
            MediaUrl = mediaUrl
        };

        // Send to BOTH receiver and sender
        await Clients.Users(receiverId.ToString(), senderId.ToString()).SendAsync("ReceiveMessage", messageObj);
    }

    // Hub Method: Send Room Message
    public async Task SendRoomMessage(int roomId, string message, string messageType = "TEXT", string? mediaUrl = null)
    {
        var senderId = int.Parse(Context.UserIdentifier!);

        var messageEvent = new MessageSentEvent
        {
            SenderId = senderId,
            RoomId = roomId,
            Content = message,
            SentAt = DateTime.UtcNow,
            MessageType = messageType,
            MediaUrl = mediaUrl
        };

        await _publishEndpoint.Publish(messageEvent);
        
        await Clients.Group(roomId.ToString()).SendAsync("ReceiveRoomMessage", new {
            RoomId = roomId,
            SenderId = senderId,
            Content = message,
            Timestamp = messageEvent.SentAt,
            MessageType = messageType,
            MediaUrl = mediaUrl
        });
    }

    public async Task SendMediaMessage(int? receiverId, int? roomId, string mediaUrl, string messageType)
    {
        var senderId = int.Parse(Context.UserIdentifier!);
        
        var messageEvent = new MessageSentEvent
        {
            SenderId = senderId,
            ReceiverId = receiverId,
            RoomId = roomId,
            Content = "[Media]",
            SentAt = DateTime.UtcNow,
            MessageType = messageType,
            MediaUrl = mediaUrl
        };

        await _publishEndpoint.Publish(messageEvent);
        
        var messageObj = new {
            SenderId = senderId,
            ReceiverId = receiverId,
            RoomId = roomId,
            Content = "[Media]",
            Timestamp = messageEvent.SentAt,
            MessageType = messageType,
            MediaUrl = mediaUrl
        };

        if (roomId.HasValue)
        {
            await Clients.Group(roomId.Value.ToString()).SendAsync("ReceiveRoomMessage", messageObj);
        }
        else if (receiverId.HasValue)
        {
            await Clients.Users(receiverId.Value.ToString(), senderId.ToString()).SendAsync("ReceiveMessage", messageObj);
        }
    }

    // Hub Method: Typing Indicator (No DB Persistence)
    public async Task TypingIndicator(int roomId, bool isTyping)
    {
        var senderId = int.Parse(Context.UserIdentifier!);
        await Clients.Group(roomId.ToString()).SendAsync("UserTyping", senderId, isTyping);
    }

    // Helper to join a room group
    public async Task JoinRoom(int roomId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, roomId.ToString());
    }

    public async Task LeaveRoom(int roomId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId.ToString());
    }

    public async Task MarkMessageRead(int messageId)
    {
        // This is handled by the Message service via REST mostly, 
        // but we can notify others here if needed.
        await Clients.Others.SendAsync("MessageRead", messageId, DateTime.UtcNow.ToString());
    }
}
