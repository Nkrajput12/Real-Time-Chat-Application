using ConnectHub.Authentication.Models.DTOs;
using ConnectHub.Authentication.Repository.Interface;
using ConnectHub.Authentication.Service.Interface;
using System.Net.Mail;
using System.Threading.Tasks;


namespace ConnectHub.Authentication.Service;

public class OAuthService : IOAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IUserService _userService;

    public OAuthService(IUserRepository userRepository, IUserService userService)
    {
        _userRepository = userRepository;
        _userService = userService;
    }

    public async Task<OAuthResultDto> ProcessLoginAsync(string email, string displayName, string provider)
    {
        var exist = await _userRepository.FindByEmailAsync(email);

        if (exist != null)
        {
            var token = await _userService.RefreshTokenAsync(exist.UserId);

            return new OAuthResultDto
            {
                Success = true,
                Message = $"Loggend in via {provider}",
                Token = token?.Token,
                UserId = exist.UserId,
                UserName = exist.UserName
            };
        }

        var baseUsername = System.Text.RegularExpressions.Regex.Replace(email.Split('@')[0], @"[^a-zA-Z0-9_]", "");
        var username = $"{baseUsername}_{provider.ToLower()}";
        if (username.Length > 20)
        {
            username = username.Substring(0, 20);
        }
        if (username.Length < 3) 
        {
            username = username.PadRight(3, '0');
        }

        var Registerdto = new RegisterRequestDto
        {
            UserName = username,
            Email = email,
            // Guid itself has hyphens, we add upper, lower, digit, and special char to ensure it passes the regex
            Password = Guid.NewGuid().ToString() + "Aa1@"
        };

        AuthResponseDto? Response = await _userService.RegisterAsync(Registerdto);

        if (Response == null)
        {
            return new OAuthResultDto
            {
                Success = false,
                Message = $"Could not create OAuth account.",
            };
        }

        var newUser = await _userRepository.FindByEmailAsync(email);
        if (newUser != null)
        {
            newUser.DisplayName = displayName;
            await _userRepository.SaveChangesAsync();
        }

        return new OAuthResultDto
        {
            Success = true,
            Message = $"Account created and logged in via {provider}.",
            Token = Response.Token,
            UserName = Response.UserName,
            UserId = Response.UserId
        };
    }

}