using MassTransit;
using ConnectHub.Shared.Events;
using ConnectHub.Messages.Services.Interface;
using ConnectHub.MessageService.Models;

namespace ConnectHub.Messaging.Consumers
{
    public class MessageSentConsumer : IConsumer<MessageSentEvent>
    {
        private readonly IMessageService _messageService;

        public MessageSentConsumer(IMessageService messageService)
        {
            _messageService = messageService;
        }

        public async Task Consume(ConsumeContext<MessageSentEvent> context)
        {
            var message = new Message
            {
                SenderId = context.Message.SenderId,
                ReceiverId = context.Message.ReceiverId,
                RoomId = context.Message.RoomId,
                Content = context.Message.Content,
                SentAt = context.Message.SentAt,
                MediaUrl = context.Message.MediaUrl,
                MessageType = context.Message.MessageType == "IMAGE" ? MessageType.IMAGE :
                              context.Message.MessageType == "FILE" ? MessageType.FILE :
                              context.Message.MessageType == "AUDIO" ? MessageType.AUDIO : MessageType.TEXT
            };

            await _messageService.SendMessageAsync(message);
        }
    }
}
