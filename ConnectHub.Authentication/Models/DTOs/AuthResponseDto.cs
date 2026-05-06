namespace ConnectHub.Authentication.Models.DTOs;

public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string Role { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }

    public AuthResponseDto(string token, string userName, int userId, string role, string displayName, string email, string? avatarUrl = null)
    {
        Token = token;
        UserName = userName;
        UserId = userId;
        Role = role;
        DisplayName = displayName;
        Email = email;
        AvatarUrl = avatarUrl;
    }
}