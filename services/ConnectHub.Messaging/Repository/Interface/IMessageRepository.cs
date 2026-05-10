using ConnectHub.MessageService.Models;

namespace ConnectHub.Messages.Repositories.Interface;

public interface IMessageRepository
{
    Task<Message?> FindByMessageIdAsync(int id);
    Task<IEnumerable<Message>> FindBySenderAndReceiverAsync(int senderId, int receiverId);
    Task<IEnumerable<Message>> FindByRoomIdAsync(int roomId);
    Task<IEnumerable<Message>> FindUnreadByReceiverIdAsync(int receiverId);
    Task<IEnumerable<Message>> FindRecentMessagesAsync(int userId);
    Task<int> CountUnreadByReceiverIdAsync(int receiverId);
    Task MarkAllReadByRoomIdAsync(int roomId, int userId);
    Task DeleteByMessageIdAsync(int messageId);
    Task<IEnumerable<Message>> SearchMessagesAsync(string query, int userId);
    Task AddMessageAsync(Message message);
    Task SaveChangesAsync();    
}