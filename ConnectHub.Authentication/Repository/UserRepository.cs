using System.Data;
using ConnectHub.Authentication.Models;
using ConnectHub.Authentication.Repository.Interface;
using ConnectHub.Authentication.Repository.Data;
using Microsoft.EntityFrameworkCore;

namespace ConnectHub.Authentication.Repository;

public class UserRepository : IUserRepository
{
    private readonly AuthDbContext _context;

    public UserRepository(AuthDbContext context)
    {
        _context = context;
    }

    public async Task<User?> FindByEmailAsync(string email)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Email == email && u.IsActive);
    }

    public async Task<User?> FindByUserIdAsync(int userId)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId && u.IsActive);
    }

    public async Task<User?> FindByUserNameAsync(string userName)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.UserName == userName && u.IsActive);
    }

    public async Task<bool> ExistsByEmailOrUserNameAsync(string emailorUserName)
    {
        return await _context.Users.AnyAsync(u => u.UserName == emailorUserName || u.Email == emailorUserName);
    }

    public async Task<IEnumerable<User>> FindAllActiveAsync()
    {
        return await _context.Users.Where(u => u.IsActive && u.IsOnline).ToListAsync();
    }

    public async Task UpdateOnlineStatusAsync(int userId, bool isOnline)
    {
        var user = await _context.Users.FindAsync(userId);
        if(user != null)
        {
            user.IsOnline = isOnline;
            user.LastSeen = DateTime.UtcNow;
            _context.Users.Update(user);
        }
    }

    public async Task<IEnumerable<User>> SearchUsersAsync(string search)
    {
        return await _context.Users.Where(u => u.UserName.Contains(search) || 
                                            u.DisplayName.Contains(search) || 
                                            u.Email.Contains(search)).ToListAsync();
    }
    public async Task<User> AddUserAsync(User user)
    {
        await _context.Users.AddAsync(user);
        return user;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}