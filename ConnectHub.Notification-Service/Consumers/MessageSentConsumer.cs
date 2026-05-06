using MassTransit;
using ConnectHub.Shared.Events;
using ConnectHub.Notification.API.Services.Interface;
using ConnectHub.Notification.API.Models;

namespace ConnectHub.Notification.API.Consumers
{
    public class MessageSentConsumer : IConsumer<MessageSentEvent>
    {
        private readonly INotificationService _notificationService;

        public MessageSentConsumer(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public async Task Consume(ConsumeContext<MessageSentEvent> context)
        {
            if (context.Message.ReceiverId.HasValue)
            {
                var notification = new ConnectHub.Notification.API.Models.Notification
                {
                    RecipientId = context.Message.ReceiverId.Value,
                    SenderId = context.Message.SenderId,
                    Title = "New Message",
                    Message = $"You received a message from User {context.Message.SenderId}",
                    Type = "MESSAGE",
                    RelatedId = context.Message.SenderId, // For DMs, link back to the sender
                    RelatedType = "USER",
                    IsRead = false,
                    SentAt = DateTime.UtcNow
                };

                await _notificationService.CreateNotificationAsync(notification);

                // Fallback for testing: If email is missing, send to a test address so it shows up in Mailtrap
                var targetEmail = !string.IsNullOrEmpty(context.Message.RecipientEmail) 
                                  ? context.Message.RecipientEmail 
                                  : "test-user@connecthub.com"; 

                // We removed the presence check as requested, so we always try to send an email for now
                await _notificationService.SendEmailAsync(
                    targetEmail, 
                    "New Message Received", 
                    $"Hello! You received a message from User {context.Message.SenderId}: {context.Message.Content}"
                );
            }
        }
    }
}
