using System.ComponentModel.DataAnnotations;
using ConnectHub.Rooms.Models;

namespace ConnectHub.Rooms.DTOs;

public class CreateRoomDto
{
    [Required]
    public string RoomName { get; set; } = string.Empty;


    public string? Description { get; set; }

    [Required]
    public RoomType RoomType { get; set; } = RoomType.PUBLIC;

    public string? AvatarUrl { get; set; }
    public int MaxMembers { get; set; } = 500;

}