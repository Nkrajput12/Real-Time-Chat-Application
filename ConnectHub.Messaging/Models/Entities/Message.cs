namespace ConnectHub.MessageService.Models;

public enum MessageType { TEXT, IMAGE, FILE, AUDIO }

public class Message
{
    public int MessageId { get; set; }
    public int SenderId { get; set; }
    public int? ReceiverId { get; set; } 
    public int? RoomId { get; set; }

    public string Content { get; set; } = string.Empty;
    public MessageType MessageType { get; set; } = MessageType.TEXT;
    public bool IsRead { get; set; } = false;
    public bool IsDeleted { get; set; } = false; 
    public bool IsEdited { get; set; } = false;
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReadAt { get; set; }
    public DateTime? EditedAt { get; set; }
    public string? MediaUrl { get; set; }
    public int? ReplyToMessageId { get; set; }
}