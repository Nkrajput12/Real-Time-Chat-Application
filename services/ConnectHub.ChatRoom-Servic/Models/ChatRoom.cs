namespace ConnectHub.Rooms.Models;
using System.ComponentModel.DataAnnotations;

public enum RoomType { PUBLIC, PRIVATE, DIRECT }
public enum MemberRole { ADMIN, MODERATOR, MEMBER }

public class ChatRoom
{
    [Key]
    public int RoomId { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public RoomType RoomType { get; set; } = RoomType.PUBLIC;
    public string? AvatarUrl { get; set; }
    public int CreatedBy { get; set; } // FK to User
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    public int MaxMembers { get; set; } = 500;

    [System.Text.Json.Serialization.JsonIgnore]
    public ICollection<RoomMember> Members { get; set; } = new List<RoomMember>();
}

