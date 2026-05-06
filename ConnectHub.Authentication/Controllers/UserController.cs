using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ConnectHub.Authentication.Service.Interface;
using ConnectHub.Authentication.Models.DTOs;
using ConnectHub.Authentication.Exceptions;
using System.Security.Claims;

namespace ConnectHub.Auth.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
        {
            try
            {
                var result = await _userService.RegisterAsync(request);

                if (result == null)
                    return BadRequest(new { message = "Registration failed. Username or Email may already be taken." });

                return Ok(result);
            }
            catch (ValidationException ex)
            {
                // Regex / format validation failed — return all error messages to the client
                return BadRequest(new { message = "Validation failed.", errors = ex.Errors });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            try
            {
                var result = await _userService.LoginAsync(request);

                if (result == null)
                    return Unauthorized(new { message = "Invalid email/username or password." });

                return Ok(result);
            }
            catch (ValidationException ex)
            {
                // Regex / format validation failed — return all error messages to the client
                return BadRequest(new { message = "Validation failed.", errors = ex.Errors });
            }
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetMyProfile()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString)) return Unauthorized();

            var user = await _userService.GetUserByIdAsync(int.Parse(userIdString));
            if (user == null) return NotFound();

            return Ok(user);
        }

        [Authorize]
        [HttpPut("Updateprofile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequestDto request)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var success = await _userService.UpdateProfileAsync(userId, request);
            
            return success ? Ok(new { message = "Profile updated successfully." }) : BadRequest();
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword(string oldPassword, string newPassword)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var success = await _userService.ChangePasswordAsync(userId, oldPassword, newPassword);
            
            return success ? Ok(new { message = "Password changed successfully." }) : BadRequest("Incorrect old password.");
        }

        [Authorize]
        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string query)
        {
            var users = await _userService.SearchUsersAsync(query);
            return Ok(users);
        }

        [Authorize]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null) return NotFound();
            return Ok(user);
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _userService.LogoutAsync(userId);
            return Ok(new { message = "Status set to offline. Client should discard token." });
        }

        [Authorize]
        [HttpGet("active")]
        public async Task<IActionResult> GetActiveUsers()
        {
            var users = await _userService.GetAllActiveUsersAsync();
            return Ok(users);
        }

        
    }
}