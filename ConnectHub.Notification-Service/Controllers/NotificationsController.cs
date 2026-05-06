using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using ConnectHub.Notification.API.Models;
using ConnectHub.Notification.API.Services.Interface;

namespace ConnectHub.Notification.API.Controllers
{
    [ApiController]
    [Route("api/notifications")]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpPost]
        public async Task<IActionResult> Create(Models.Notification notification)
        {
            var created = await _notificationService.CreateNotificationAsync(notification);
            return Ok(created);
        }

        [HttpGet("{userId}")]
        public async Task<IActionResult> GetNotifications(int userId, [FromQuery] int page = 1, [FromQuery] int size = 50)
        {
            var notifications = await _notificationService.GetNotificationsAsync(userId, page, size);
            return Ok(notifications);
        }

        [HttpGet("unread-count/{userId}")]
        public async Task<IActionResult> GetUnreadCount(int userId)
        {
            var count = await _notificationService.GetUnreadCountAsync(userId);
            return Ok(new { Count = count });
        }

        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkRead(int id)
        {
            var success = await _notificationService.MarkAsReadAsync(id);
            if (!success) return NotFound();
            return Ok();
        }

        [HttpPut("read-all")]
        public async Task<IActionResult> MarkAllRead()
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            await _notificationService.MarkAllAsReadAsync(userId);
            return Ok();
        }

        [HttpPost("broadcast")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Broadcast([FromBody] string message)
        {
            await _notificationService.SendBulkAsync("Platform Update", message);
            return Ok();
        }

        [HttpGet("test-email/{email}")]
        [AllowAnonymous] 
        public async Task<IActionResult> TestEmail(string email)
        {
            await _notificationService.SendEmailAsync(email, "ConnectHub MailKit Test", "If you see this, your email service is working perfectly!");
            return Ok("Test email dispatched. Check your Mailtrap Sandbox Inbox!");
        }
    }
}
