using ConnectHub.Rooms.Models;
using ConnectHub.Rooms.Repositories.Interface;
using ConnectHub.Rooms.Services.Interface;

namespace ConnectHub.Rooms.Services;

public class ChatRoomService : IChatRoomService
{
    private readonly IChatRoomRepository _repo;

    public ChatRoomService(IChatRoomRepository repo)
    {
        _repo = repo;
    }

    public async Task<ChatRoom> CreateRoomAsync(ChatRoom room)
    {
        await _repo.AddRoomAsync(room);
        await _repo.SaveChangesAsync();
        
        var adminMember = new RoomMember
        {
            RoomId = room.RoomId,
            UserId = room.CreatedBy,
            Role = MemberRole.ADMIN,
            JoinedAt = DateTime.UtcNow,
            IsActive = true
        };

        await _repo.AddMemberAsync(adminMember);
        await _repo.SaveChangesAsync();
        
        return room;
    }
    public async Task<RoomMember?> GetMemberById(int userId)
    {
        return await _repo.GetMemberById(userId);
    }

    public async Task<ChatRoom?> GetRoomByIdAsync(int roomId)
    {
        return await _repo.FindByRoomIdAsync(roomId);
    }

    public async Task<IEnumerable<ChatRoom>> GetRoomsByUserAsync(int userId)
    {
        return await _repo.FindRoomsByUserIdAsync(userId);
    }

    public async Task<IEnumerable<ChatRoom>> GetPublicRoomsAsync()
    {
        return await _repo.FindPublicRoomsAsync();
    }

    public async Task<bool> AddMemberAsync(int roomId, int userId, MemberRole role = MemberRole.MEMBER)
    {
        var room = await _repo.FindByRoomIdAsync(roomId);
        if (room == null || !room.IsActive) return false;

        var currentCount = await _repo.CountMembersByRoomIdAsync(roomId);
        if (currentCount >= room.MaxMembers)
            throw new InvalidOperationException("Chat room capacity reached.");

        if (await _repo.IsUserInRoomAsync(roomId, userId)) return false;

        var member = new RoomMember
        {
            RoomId = roomId,
            UserId = userId,
            Role = role,
            JoinedAt = DateTime.UtcNow,
            IsActive = true
        };

        await _repo.AddMemberAsync(member);
        await _repo.SaveChangesAsync();
        return true;
    }

    public async Task<bool> LeaveRoomAsync(int roomId, int userId)
    {
        return await RemoveMemberAsync(roomId, userId);
    }

    public async Task<bool> RemoveMemberAsync(int roomId, int userId)
    {
        var isMember = await _repo.IsUserInRoomAsync(roomId, userId);
        if (!isMember) return false;

        await _repo.RemoveMemberAsync(roomId, userId);
        await _repo.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<RoomMember>> GetMembersAsync(int roomId)
    {
        return await _repo.FindMembersByRoomIdAsync(roomId);
    }

    public async Task<bool> UpdateMemberRoleAsync(int roomId, int userId, MemberRole newRole)
    {
        var members = await _repo.FindMembersByRoomIdAsync(roomId);
        var member = members.FirstOrDefault(m => m.UserId == userId);
        
        if (member == null) return false;

        member.Role = newRole;
        await _repo.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateRoomAsync(int roomId, ChatRoom updatedRoom)
    {
        var room = await _repo.FindByRoomIdAsync(roomId);
        if (room == null) return false;

        room.RoomName = updatedRoom.RoomName;
        room.Description = updatedRoom.Description;
        room.RoomType = updatedRoom.RoomType;
        room.AvatarUrl = updatedRoom.AvatarUrl;

        await _repo.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteRoomAsync(int roomId)
    {
        var room = await _repo.FindByRoomIdAsync(roomId);
        if (room == null) return false;

        room.IsActive = false; 
        await _repo.SaveChangesAsync();
        return true;
    }

    public async Task<bool> IsUserInRoomAsync(int roomId, int userId)
    {
        return await _repo.IsUserInRoomAsync(roomId, userId);
    }

    public async Task<int> GetMemberCountAsync(int roomId)
    {
        return await _repo.CountMembersByRoomIdAsync(roomId);
    }

    
}