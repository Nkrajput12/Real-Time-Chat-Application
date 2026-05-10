namespace ConnectHub.Rooms.Models;
using System.ComponentModel.DataAnnotations;

public class RoomMember
{
    [Key]
    public int MemberId { get; set; }
    public int RoomId { get; set; }
    public int UserId { get; set; }
    public MemberRole Role { get; set; } = MemberRole.MEMBER;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    [System.Text.Json.Serialization.JsonIgnore]
    public ChatRoom Room { get; set; } = null!;
}