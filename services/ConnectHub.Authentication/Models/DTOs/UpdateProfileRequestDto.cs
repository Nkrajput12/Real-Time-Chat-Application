namespace ConnectHub.Authentication.Models.DTOs;

public class UpdateProfileRequestDto
{
    public string? DisplayName {get; set;}
    public string? Bio {get; set;}
    public string? AvatarUrl{get; set;}
}