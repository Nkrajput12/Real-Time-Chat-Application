using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using MailKit.Net.Smtp;
using MimeKit;
using ConnectHub.Notification.API.Models;
using ConnectHub.Notification.API.Models.Interface;
using ConnectHub.Notification.API.Services.Interface;

namespace ConnectHub.Notification.API.Services
{
    
    public class ChatHub : Hub { }

    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _repo;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;

        public NotificationService(INotificationRepository repo, IHubContext<ChatHub> hubContext, IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _repo = repo;
            _hubContext = hubContext;
            _httpClientFactory = httpClientFactory;
            _config = config;
        }

        public async Task<Models.Notification> CreateNotificationAsync(Models.Notification notification)
        {
            var created = await _repo.AddNotificationAsync(notification);
            
            var unreadCount = await _repo.GetUnreadCountAsync(notification.RecipientId);
            
            // Real-time update via SignalR
            await _hubContext.Clients.User(notification.RecipientId.ToString()).SendAsync("NotificationCount", unreadCount);

            return created;
        }

        public async Task<IEnumerable<Models.Notification>> GetNotificationsAsync(int userId, int page, int size)
        {
            return await _repo.GetNotificationsByRecipientAsync(userId, page, size);
        }

        public async Task<int> GetUnreadCountAsync(int userId)
        {
            return await _repo.GetUnreadCountAsync(userId);
        }

        public async Task<bool> MarkAsReadAsync(int id)
        {
            return await _repo.MarkAsReadAsync(id);
        }

        public async Task<bool> MarkAllAsReadAsync(int userId)
        {
            return await _repo.MarkAllAsReadAsync(userId);
        }

        public async Task SendBulkAsync(string title, string message)
        {
            await _hubContext.Clients.All.SendAsync("BroadcastNotification", title, message);
        }

        public async Task<bool> IsUserOnlineAsync(int userId)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var presenceUrl = _config["PresenceServiceUrl"] ?? "http://localhost:5005";
                var response = await client.GetAsync($"{presenceUrl}/api/presence/check/{userId}");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return bool.Parse(content);
                }
            }
            catch { /* Fallback to sending email if presence service is down */ }
            return false;
        }

        public async Task SendEmailAsync(string recipientEmail, string subject, string body)
        {
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress("ConnectHub", _config["Email:From"] ?? "noreply@connecthub.com"));
            email.To.Add(new MailboxAddress("User", recipientEmail)); 
            email.Subject = subject;
            email.Body = new TextPart("plain") { Text = body };

            try
            {
                using var smtp = new SmtpClient();
                await smtp.ConnectAsync(_config["Email:Host"], int.Parse(_config["Email:Port"] ?? "587"), MailKit.Security.SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(_config["Email:User"], _config["Email:Pass"]);
                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Email sending failed: {ex.Message}");
            }
        }
    }
}
