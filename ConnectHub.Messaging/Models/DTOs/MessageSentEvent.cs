using System;

namespace ConnectHub.Shared.Events
{
    public class MessageSentEvent
    {
        public int SenderId { get; set; }
        public int? ReceiverId { get; set; }
        public int? RoomId { get; set; }
        public string Content { get; set; } = string.Empty;
        public string? RecipientEmail { get; set; }
        public string MessageType { get; set; } = "TEXT";
        public string? MediaUrl { get; set; }
        public DateTime SentAt { get; set; }
    }
}
