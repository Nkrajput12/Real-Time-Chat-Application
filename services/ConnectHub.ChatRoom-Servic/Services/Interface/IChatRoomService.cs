using ConnectHub.Rooms.Models;

namespace ConnectHub.Rooms.Services.Interface;

public interface IChatRoomService
{
    Task<ChatRoom> CreateRoomAsync(ChatRoom room);
    Task<ChatRoom?> GetRoomByIdAsync(int roomId);
    Task<IEnumerable<ChatRoom>> GetRoomsByUserAsync(int userId);
    Task<IEnumerable<ChatRoom>> GetPublicRoomsAsync();
    Task<bool> UpdateRoomAsync(int roomId, ChatRoom updatedRoom);
    Task<bool> DeleteRoomAsync(int roomId);

    Task<bool> AddMemberAsync(int roomId, int userId, MemberRole role = MemberRole.MEMBER);
    Task<bool> RemoveMemberAsync(int roomId, int userId);
    Task<bool> LeaveRoomAsync(int roomId, int userId);
    Task<IEnumerable<RoomMember>> GetMembersAsync(int roomId);
    Task<bool> UpdateMemberRoleAsync(int roomId, int userId, MemberRole newRole);

    Task<bool> IsUserInRoomAsync(int roomId, int userId);
    Task<int> GetMemberCountAsync(int roomId);
    Task<RoomMember?> GetMemberById(int userId);
}