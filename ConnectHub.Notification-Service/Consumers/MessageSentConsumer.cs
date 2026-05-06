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

                //if the user is OFFLINE
                if (!string.IsNullOrEmpty(context.Message.RecipientEmail))
                {
                    bool isOnline = await _notificationService.IsUserOnlineAsync(context.Message.ReceiverId.Value);
                    
                    if (!isOnline)
                    {
                        await _notificationService.SendEmailAsync(
                            context.Message.RecipientEmail, 
                            "New Message Received (Offline)", 
                            $"Hello! You received a message from User {context.Message.SenderId} while you were away: {context.Message.Content}"
                        );
                    }
                }
            }
        }
    }
}
