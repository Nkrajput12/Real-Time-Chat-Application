namespace ConnectHub.Authentication.Models.DTOs;

public class OAuthResultDto
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string? Token { get; set; }
    public int UserId { get; set; }
    public string? UserName { get; set; }
    public string? Message { get; set; }
}