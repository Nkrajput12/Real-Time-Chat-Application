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

        public Task<bool> IsUserOnlineAsync(int userId)
        {
            // Removed synchronous call to Presence Service to keep services decoupled.
            // Notifications will default to email sending.
            return Task.FromResult(false);
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
                
                // BYPASS SSL VALIDATION (Common fix for Mailtrap/Docker)
                smtp.ServerCertificateValidationCallback = (s, c, h, e) => true;

                smtp.Timeout = 10000; // 10 seconds

                var host = _config["Email:Host"];
                var port = int.Parse(_config["Email:Port"] ?? "587");
                
                Console.WriteLine($"Attempting to send email via {host}:{port}...");
                
                await smtp.ConnectAsync(host, port, MailKit.Security.SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(_config["Email:User"], _config["Email:Pass"]);
                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);
                
                Console.WriteLine($"Email sent successfully to {recipientEmail}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CRITICAL EMAIL FAILURE for {recipientEmail}: {ex.Message}");
                // We don't throw here to avoid crashing the whole notification flow if SMTP fails
            }
        }
    }
}
