using ConnectHub.MessageService.Models;
using ConnectHub.Messages.Repository.Data;
using Microsoft.EntityFrameworkCore;
using ConnectHub.Messages.Repositories.Interface;

namespace ConnectHub.Messages.Repositories;

public class MessageRepository : IMessageRepository
{
    private readonly MessageDbContext _context;

    public MessageRepository(MessageDbContext context)
    {
        _context = context;
    }
    public async Task<Message?> FindByMessageIdAsync(int id)
    {
        return await _context.Messages.FirstOrDefaultAsync(m => m.MessageId == id && !m.IsDeleted);
    }
    public async Task<IEnumerable<Message>> FindBySenderAndReceiverAsync(int senderId, int receiverId)
    {
        return await _context.Messages
            .Where(m => !m.IsDeleted &&
                ((m.SenderId == senderId && m.ReceiverId == receiverId) ||
                (m.SenderId == receiverId && m.ReceiverId == senderId)))
            .OrderBy(m => m.SentAt).ToListAsync();
    }
    public async Task<IEnumerable<Message>> FindByRoomIdAsync(int roomId)
    {
        return await _context.Messages.Where(m => m.RoomId == roomId && !m.IsDeleted)
            .OrderBy(m => m.SentAt).ToListAsync();
    }
    public async Task<IEnumerable<Message>> FindUnreadByReceiverIdAsync(int receiverId)
    {
        return await _context.Messages.Where(m => m.ReceiverId == receiverId && !m.IsRead && !m.IsDeleted).ToListAsync();
    }
    public async Task<IEnumerable<Message>> FindRecentMessagesAsync(int userId)
    {
        var messages = await _context.Messages
            .Where(m => (m.SenderId == userId || m.ReceiverId == userId) && !m.IsDeleted)
            .ToListAsync();

        return messages
            .GroupBy(m => m.SenderId == userId ? m.ReceiverId : m.SenderId)
            .Select(g => g.OrderByDescending(m => m.SentAt).First())
            .OrderByDescending(m => m.SentAt)
            .ToList();
    }
    public async Task<int> CountUnreadByReceiverIdAsync(int receiverId)
    {
        return await _context.Messages.CountAsync(m => m.ReceiverId == receiverId && !m.IsRead && !m.IsDeleted);
    }
    public async Task MarkAllReadByRoomIdAsync(int roomId, int userId)
    {
        var unread = await _context.Messages
            .Where(m => m.RoomId == roomId && m.ReceiverId == userId && !m.IsRead).ToListAsync();
        unread.ForEach(m => { m.IsRead = true; m.ReadAt = DateTime.UtcNow; });
    }

    public async Task DeleteByMessageIdAsync(int messageId)
    {
        var msg = await _context.Messages.FindAsync(messageId);
        if (msg != null) msg.IsDeleted = true;
    }

    public async Task<IEnumerable<Message>> SearchMessagesAsync(string query,int userId)
    {
        return await _context.Messages
            .Where(m => (m.SenderId == userId || m.ReceiverId == userId) &&
                        m.Content.Contains(query) && !m.IsDeleted)
            .ToListAsync();
    }
    public async Task AddMessageAsync(Message message)
    {
        await _context.Messages.AddAsync(message);
    }
    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}