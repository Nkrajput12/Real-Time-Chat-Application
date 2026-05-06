using ConnectHub.Rooms.Models;

namespace ConnectHub.Rooms.Repositories.Interface;

public interface IChatRoomRepository
{
    Task<ChatRoom?> FindByRoomIdAsync(int id);
    Task<IEnumerable<ChatRoom>> FindByCreatedByAsync(int userId);
    Task<IEnumerable<ChatRoom>> FindByRoomNameAsync(string roomName);
    Task<IEnumerable<ChatRoom>> FindRoomsByUserIdAsync(int userId);
    Task<IEnumerable<RoomMember>> FindMembersByRoomIdAsync(int roomId);
    Task<bool> IsUserInRoomAsync(int roomId, int userId);
    Task<int> CountMembersByRoomIdAsync(int roomId);
    Task<IEnumerable<ChatRoom>> FindPublicRoomsAsync();
    Task AddRoomAsync(ChatRoom room);
    Task AddMemberAsync(RoomMember member);
    Task RemoveMemberAsync(int roomId, int userId);
    Task SaveChangesAsync();
    Task<RoomMember?> GetMemberById(int userId);
}