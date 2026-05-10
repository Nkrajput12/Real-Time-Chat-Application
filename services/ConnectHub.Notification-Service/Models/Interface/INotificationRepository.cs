using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ConnectHub.Notification.API.Models.Interface
{
    public interface INotificationRepository
    {
        Task<Notification?> GetNotificationByIdAsync(int id);
        Task<IEnumerable<Notification>> GetNotificationsByRecipientAsync(int userId, int page, int size);
        Task<int> GetUnreadCountAsync(int userId);
        Task<Notification> AddNotificationAsync(Notification notification);
        Task<bool> MarkAsReadAsync(int id);
        Task<bool> MarkAllAsReadAsync(int userId);
        Task SaveChangesAsync();
    }
}
