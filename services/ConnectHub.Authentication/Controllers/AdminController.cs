using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ConnectHub.Authentication.Service.Interface;
using ConnectHub.Authentication.Models.DTOs;

namespace ConnectHub.Auth.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize(Roles = "Admin")] 
    public class AdminController : ControllerBase
    {
        private readonly IUserService _userService;

        public AdminController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet("Get-all-users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _userService.GetAllActiveUsersAsync();
            return Ok(users);
        }


        [HttpGet("users/{username}")]
        public async Task<IActionResult> GetUserDetail(string username)
        {
            var user = await _userService.GetUserByUserNameAsync(username);
            return user != null ? Ok(user) : NotFound();
        }

        [HttpPost("deactivate/{id}")]
        public async Task<IActionResult> DeactivateUser(int id)
        {
            var success = await _userService.DeactivateAccountAsync(id);
            if (!success) return NotFound(new { message = "User not found or already inactive." });
            
            return Ok(new { message = $"User with ID {id} has been deactivated." });
        }

        [HttpPost("status")]
        public async Task<IActionResult> ForceUserOffline(int userId)
        {
            await _userService.SetOnlineStatusAsync(userId, false);
            return Ok(new { message = "User status updated to offline." });
        }
    }
}