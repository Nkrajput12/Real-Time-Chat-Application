using System;

namespace ConnectHub.Notification.API.Models
{
    public class Notification
    {
        public int NotificationId { get; set; }
        public int RecipientId { get; set; }
        public int? SenderId { get; set; }
        public string Type { get; set; } = string.Empty; // MESSAGE/MENTION/ROOM_INVITE/ROLE_CHANGE/PLATFORM
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public int? RelatedId { get; set; }
        public string? RelatedType { get; set; }
        public bool IsRead { get; set; } = false;
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
    }
}
