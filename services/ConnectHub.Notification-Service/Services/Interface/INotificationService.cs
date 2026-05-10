using System.Collections.Generic;
using System.Threading.Tasks;
using ConnectHub.Notification.API.Models;

namespace ConnectHub.Notification.API.Services.Interface
{
    public interface INotificationService
    {
        Task<Models.Notification> CreateNotificationAsync(Models.Notification notification);
        Task<IEnumerable<Models.Notification>> GetNotificationsAsync(int userId, int page, int size);
        Task<int> GetUnreadCountAsync(int userId);
        Task<bool> MarkAsReadAsync(int id);
        Task<bool> MarkAllAsReadAsync(int userId);
        Task SendBulkAsync(string title, string message);
        Task SendEmailAsync(string recipientEmail, string subject, string body);
        Task<bool> IsUserOnlineAsync(int userId);
    }
}
