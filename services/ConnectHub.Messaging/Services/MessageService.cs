using ConnectHub.MessageService.Models;
using ConnectHub.Messages.Repositories.Interface;
using ConnectHub.Messages.Services.Interface;

namespace ConnectHub.Messages.Services;

public class MessageService : IMessageService
{
    private readonly IMessageRepository _repo;

    public MessageService(IMessageRepository repo)
    {
        _repo = repo;
    }

    public async Task<Message> SendMessageAsync(Message message)
    {
        await _repo.AddMessageAsync(message);
        await _repo.SaveChangesAsync();
        return message;
    }

    public async Task<Message?> GetMessageByIdAsync(int id)
    {
        return await _repo.FindByMessageIdAsync(id);
    }
    public async Task<IEnumerable<Message>> GetDirectMessagesAsync(int userA, int userB)
    {
        return await _repo.FindBySenderAndReceiverAsync(userA, userB);
    }
    public async Task<IEnumerable<Message>> GetRoomMessagesAsync(int roomId)
    {
        return await _repo.FindByRoomIdAsync(roomId);
    }
    public async Task<IEnumerable<Message>> GetUnreadMessagesAsync(int userId)
    {
        return await _repo.FindUnreadByReceiverIdAsync(userId);
    }
    public async Task MarkAsReadAsync(int messageId)
    {
        var message = await _repo.FindByMessageIdAsync(messageId);
        if (message != null && !message.IsRead)
        {
            message.IsRead = true;
            message.ReadAt = DateTime.UtcNow;
            await _repo.SaveChangesAsync();
        }
    }

    public async Task MarkAllAsReadAsync(int userId, int? roomId)
    {
        if (roomId.HasValue)
            await _repo.MarkAllReadByRoomIdAsync(roomId.Value, userId);

        await _repo.SaveChangesAsync();
    }

    public async Task<bool> EditMessageAsync(int id, string newContent)
    {
        var message = await _repo.FindByMessageIdAsync(id);
        if (message == null) return false;

        message.Content = newContent;
        message.IsEdited = true;
        message.EditedAt = DateTime.UtcNow;

        await _repo.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteMessageAsync(int messageId)
    {
        var message = await _repo.FindByMessageIdAsync(messageId);
        if (message == null) return false;

        message.IsDeleted = true; 
        await _repo.SaveChangesAsync();
        return true;
    }

    public async Task<int> GetUnreadCountAsync(int userId)
    {
        return await _repo.CountUnreadByReceiverIdAsync(userId);
    }
    public async Task<IEnumerable<Message>> GetRecentChatsAsync(int userId)
    { return await _repo.FindRecentMessagesAsync(userId);
    }
    public async Task<IEnumerable<Message>> SearchMessagesAsync(string query, int userId){
        return await _repo.SearchMessagesAsync(query, userId);
    }
}