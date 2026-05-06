using ConnectHub.MessageService.Models;
namespace ConnectHub.Messages.Services.Interface;

public interface IMessageService
{
    Task<Message> SendMessageAsync(Message message);
    Task<Message?> GetMessageByIdAsync(int id);
    Task<IEnumerable<Message>> GetDirectMessagesAsync(int userA, int userB);
    Task<IEnumerable<Message>> GetRoomMessagesAsync(int roomId);
    Task<IEnumerable<Message>> GetUnreadMessagesAsync(int userId);
    Task MarkAsReadAsync(int messageId);
    Task MarkAllAsReadAsync(int userId, int? roomId);
    Task<bool> EditMessageAsync(int id, string newContent);
    Task<bool> DeleteMessageAsync(int messageId);
    Task<int> GetUnreadCountAsync(int userId);
    Task<IEnumerable<Message>> GetRecentChatsAsync(int userId);
    Task<IEnumerable<Message>> SearchMessagesAsync(string query, int userId);
}