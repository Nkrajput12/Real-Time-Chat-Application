using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ConnectHub.Authentication.Service.Interface;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace ConnectHub.Auth.Controllers;

[ApiController]
[Route("auth")]
public class OAuthController : ControllerBase
{
    private readonly IOAuthService _oauthService;

    public OAuthController(IOAuthService oAuthService)
    {
        _oauthService = oAuthService;
    }

    [HttpGet("google-login")]
    public IActionResult GoogleLogin()
    {
        var redirect = Url.Action(nameof(GoogleCallback), "OAuth");
        return Challenge(new AuthenticationProperties { RedirectUri = redirect },
                        GoogleDefaults.AuthenticationScheme);
    }

    [HttpGet("google-callback")]
    public async Task<IActionResult> GoogleCallback()
    {
        var result = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);
        if (!result.Succeeded)
            return Unauthorized(new { message = "Google authentication failed." });

        var email = result.Principal?.FindFirstValue(ClaimTypes.Email);
        var name = result.Principal?.FindFirstValue(ClaimTypes.Name) ?? email ?? "user";

        if (string.IsNullOrEmpty(email))
            return BadRequest(new { message = "Could not retrieve email from Google." });

        var login = await _oauthService.ProcessLoginAsync(email, name, "Google");
        return RedirectToFrontend(login.Token, login.UserName, login.UserId);
    }

    private IActionResult RedirectToFrontend(string? token, string? userName, int userId)
    {
        // Redirect back to frontend with info
        var frontendUrl = $"http://localhost:4200/auth/login?token={token}&user={userName}&userId={userId}";
        return Redirect(frontendUrl);
    }
}