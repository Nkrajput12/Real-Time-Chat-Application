using ConnectHub.Rooms.Models;
using ConnectHub.Rooms.Data;
using Microsoft.EntityFrameworkCore;
using ConnectHub.Rooms.Repositories.Interface;

namespace ConnectHub.Rooms.Repositories;

public class ChatRoomRepository : IChatRoomRepository
{
    private readonly RoomDbContext _context;

    public ChatRoomRepository(RoomDbContext context)
    {
        _context = context;
    }

    public async Task<ChatRoom?> FindByRoomIdAsync(int id)
    {
        return await _context.ChatRooms
            .Include(r => r.Members)
            .FirstOrDefaultAsync(r => r.RoomId == id && r.IsActive);
    }

    public async Task<IEnumerable<ChatRoom>> FindByCreatedByAsync(int userId)
    {
        return await _context.ChatRooms
            .Where(r => r.CreatedBy == userId && r.IsActive)
            .ToListAsync();
    }

    public async Task<IEnumerable<ChatRoom>> FindByRoomNameAsync(string roomName)
    {
        return await _context.ChatRooms
            .Where(r => r.RoomName.Contains(roomName) && r.IsActive)
            .ToListAsync();
    }

    public async Task<IEnumerable<ChatRoom>> FindRoomsByUserIdAsync(int userId)
    {
        return await _context.RoomMembers
            .Where(rm => rm.UserId == userId && rm.IsActive)
            .Select(rm => rm.Room)
            .Where(r => r.IsActive)
            .ToListAsync();
    }

    public async Task<IEnumerable<RoomMember>> FindMembersByRoomIdAsync(int roomId)
    {
        return await _context.RoomMembers
            .Where(rm => rm.RoomId == roomId && rm.IsActive)
            .ToListAsync();
    }

    public async Task<bool> IsUserInRoomAsync(int roomId, int userId)
    {
        return await _context.RoomMembers
            .AnyAsync(rm => rm.RoomId == roomId && rm.UserId == userId && rm.IsActive);
    }

    public async Task<int> CountMembersByRoomIdAsync(int roomId)
    {
        return await _context.RoomMembers
            .CountAsync(rm => rm.RoomId == roomId && rm.IsActive);
    }

    public async Task<IEnumerable<ChatRoom>> FindPublicRoomsAsync()
    {
        return await _context.ChatRooms
            .Where(r => r.RoomType == RoomType.PUBLIC && r.IsActive)
            .ToListAsync();
    }

    public async Task AddRoomAsync(ChatRoom room)
    {
        await _context.ChatRooms.AddAsync(room);
    }

    public async Task AddMemberAsync(RoomMember member)
    {
        await _context.RoomMembers.AddAsync(member);
    }

    public async Task RemoveMemberAsync(int roomId, int userId)
    {
        var member = await _context.RoomMembers
            .FirstOrDefaultAsync(rm => rm.RoomId == roomId && rm.UserId == userId);
        
        if (member != null)
        {
            member.IsActive = false;
        }
    }

    public async Task<RoomMember?> GetMemberById(int userId)
    {
        return await _context.RoomMembers.FirstOrDefaultAsync(rm => rm.UserId == userId);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}