using ConnectHub.Authentication.Models;
using ConnectHub.Authentication.Models.DTOs;

namespace ConnectHub.Authentication.Service.Interface;

public interface IUserService
{
    Task<AuthResponseDto?> RegisterAsync(RegisterRequestDto request);
    Task<AuthResponseDto?> LoginAsync(LoginRequestDto request);
    Task LogoutAsync(int userId);
    Task<bool> ValidateTokenAsync(string token);
    Task<AuthResponseDto?> RefreshTokenAsync(int userId);

    Task<UserResponseDto?> GetUserByIdAsync(int userId);
    Task<UserResponseDto?> GetUserByUserNameAsync(string userName);

    Task<bool> UpdateProfileAsync(int userId, UpdateProfileRequestDto request);
    Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword);

    Task<IEnumerable<UserResponseDto>> SearchUsersAsync(string query);
    Task<bool> SetOnlineStatusAsync(int userId, bool isOnline);
    Task<IEnumerable<UserResponseDto>> GetAllActiveUsersAsync();
    Task<bool> DeactivateAccountAsync(int userId);
}