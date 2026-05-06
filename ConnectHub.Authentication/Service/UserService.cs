using System.Runtime.CompilerServices;
using ConnectHub.Authentication.Models;
using ConnectHub.Authentication.Models.DTOs;
using ConnectHub.Authentication.Repository.Interface;
using ConnectHub.Authentication.Service.Interface;
using ConnectHub.Authentication.Helpers;
using ConnectHub.Authentication.Exceptions;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace ConnectHub.Authentication.Service;

public class UserService : IUserService
{
    private readonly IUserRepository _repo;
    private readonly IConfiguration _config;
    private readonly PasswordHasher<User> _PasswordHasher;
    public UserService(IUserRepository repo, IConfiguration config)
    {
        _repo = repo;
        _config = config;
        _PasswordHasher = new PasswordHasher<User>();

    }

    public async Task<AuthResponseDto?> RegisterAsync(RegisterRequestDto register)
    {
        // ── Step 1: Regex validation ───────────────────────────────────────────
        var validationErrors = ValidationHelper.ValidateRegistration(
            register.UserName,
            register.DisplayName,
            register.Email,
            register.Password);

        if (validationErrors.Count > 0)
            throw new ValidationException(validationErrors);

        // ── Step 2: Uniqueness check ──────────────────────────────────────────
        if (await _repo.ExistsByEmailOrUserNameAsync(register.UserName)
            || await _repo.ExistsByEmailOrUserNameAsync(register.Email))
        {
            return null;
        }

        // ── Step 3: Persist user ──────────────────────────────────────────────
        var user = new User
        {
            UserName = register.UserName,
            Email = register.Email,
            DisplayName = string.IsNullOrEmpty(register.DisplayName)
                ? register.UserName
                : register.DisplayName,
            CreatedAt = DateTime.UtcNow
        };

        user.PasswordHash = _PasswordHasher.HashPassword(user, register.Password);

        await _repo.AddUserAsync(user);
        await _repo.SaveChangesAsync();

        return new AuthResponseDto(GenerateJwtToken(user), user.UserName, user.UserId, user.Role, user.DisplayName, user.Email, user.AvatarUrl);
    }

    public async Task<AuthResponseDto?> LoginAsync(LoginRequestDto request)
    {
        // ── Step 1: Regex validation ───────────────────────────────────────────
        var validationErrors = ValidationHelper.ValidateLogin(request.Email, request.Password);

        if (validationErrors.Count > 0)
            throw new ValidationException(validationErrors);

        var user = await _repo.FindByEmailAsync(request.Email)
                   ?? await _repo.FindByUserNameAsync(request.Email);

        if (user == null)
            return null; 

        // ── Step 3: Check account is active ──────────────────────────────────
        if (!user.IsActive)
            return null; 

        // ── Step 4: Verify password ──────────────────────────────────────────
        var verification = _PasswordHasher.VerifyHashedPassword(
            user, user.PasswordHash!, request.Password);

        if (verification == PasswordVerificationResult.Failed)
            return null;

        // ── Step 5: Issue token & set online ─────────────────────────────────
        var token = GenerateJwtToken(user);
        await SetOnlineStatusAsync(user.UserId, true);
        return new AuthResponseDto(token, user.UserName, user.UserId, user.Role, user.DisplayName, user.Email, user.AvatarUrl);
    }

    public async Task LogoutAsync(int userId)
    {
        await SetOnlineStatusAsync(userId, false);
    }

    public async Task<UserResponseDto?> GetUserByIdAsync(int userId)
    {
        var user = await _repo.FindByUserIdAsync(userId);
        if (user == null)
        {
            throw new UserNotFoundException(userId);
        }
        return MapToDto(user);
    }

    public async Task<UserResponseDto?> GetUserByUserNameAsync(string userName)
    {
        var user = await _repo.FindByUserNameAsync(userName);
        if (user == null)
        {
            throw new UserNotFoundException(userName, true);
        }
        return MapToDto(user);
    }

    public async Task<IEnumerable<UserResponseDto>> SearchUsersAsync(string search)
    {
        var users = await _repo.SearchUsersAsync(search);
        return users.Select(MapToDto);
    }

    public async Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword)
    {
        var user = await _repo.FindByUserIdAsync(userId);
        if (user == null)
        {
            return false;
        }

        var varifyOld = _PasswordHasher.VerifyHashedPassword(user, user.PasswordHash!, oldPassword);

        if (varifyOld == PasswordVerificationResult.Failed)
        {
            return false;
        }

        user.PasswordHash = _PasswordHasher.HashPassword(user, newPassword);
        await _repo.SaveChangesAsync();
        return true;

    }
    public async Task<bool> SetOnlineStatusAsync(int userId, bool isOnline)
    {
        await _repo.UpdateOnlineStatusAsync(userId, isOnline);
        await _repo.SaveChangesAsync();

        return true;
    }

    public async Task<IEnumerable<UserResponseDto>> GetAllActiveUsersAsync()
    {
        var users = await _repo.FindAllActiveAsync();
        return users.Select(MapToDto);
    }

    public async Task<bool> DeactivateAccountAsync(int userId)
    {
        var user = await _repo.FindByUserIdAsync(userId);
        if (user == null)
        {
            return false;
        }

        user.IsActive = false;
        await _repo.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateProfileAsync(int userId, UpdateProfileRequestDto request)
    {
        var user = await _repo.FindByUserIdAsync(userId);
        if (user == null) return false;

        user.DisplayName = request.DisplayName ?? user.DisplayName;
        user.Bio = request.Bio ?? user.Bio;
        user.AvatarUrl = request.AvatarUrl ?? user.AvatarUrl;

        await _repo.SaveChangesAsync();
        return true;
    }


    //Genetate the Jwt token
    private string GenerateJwtToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: credentials
            );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<bool> ValidateTokenAsync(string token)
    {
        var tokenhandler = new JwtSecurityTokenHandler();
        try
        {
            tokenhandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!)),
                ValidateIssuer = true,
                ValidIssuer = _config["Jwt:Issuer"],
                ValidateAudience = true,
                ValidAudience = _config["Jwt:Audience"],
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<AuthResponseDto?> RefreshTokenAsync(int userId)
    {
        var user = await _repo.FindByUserIdAsync(userId);
        return user != null ? new AuthResponseDto(GenerateJwtToken(user), user.UserName, user.UserId, user.Role, user.DisplayName, user.Email, user.AvatarUrl) : null;
    }

    private UserResponseDto MapToDto(User user)
    {
        return new UserResponseDto
        {
            UserId = user.UserId,
            UserName = user.UserName,
            DisplayName = user.DisplayName,
            Email = user.Email,
            AvatarUrl = user.AvatarUrl,
            Bio = user.Bio,
            IsOnline = user.IsOnline,
            LastSeen = user.LastSeen,
            CreatedAt = user.CreatedAt,
            Role = user.Role
        };
    }
}