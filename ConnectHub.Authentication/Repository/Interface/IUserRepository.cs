using Microsoft.EntityFrameworkCore;
using ConnectHub.Authentication.Models;

namespace ConnectHub.Authentication.Repository.Interface;

    public interface IUserRepository
    {
        Task<User?> FindByEmailAsync(string email);
        Task<User?> FindByUserIdAsync(int userId);
        Task<User?> FindByUserNameAsync(string userName);
        Task<bool> ExistsByEmailOrUserNameAsync(string emailorUserName);
        Task<IEnumerable<User>> FindAllActiveAsync();
        Task UpdateOnlineStatusAsync(int userId, bool isOnline);
        Task<IEnumerable<User>> SearchUsersAsync(string search);
        Task<User> AddUserAsync(User user);
        Task SaveChangesAsync();
    }
