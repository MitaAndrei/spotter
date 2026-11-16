using gymappAPI.Models;

namespace gymappAPI.DTOs;

public class ChatMessageDto
{
    public Guid Id { get; set; }
    public Guid SenderId { get; set; }
    public Guid ReceiverId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }

    public static ChatMessageDto From(ChatMessage message) => new()
    {
        Id = message.Id,
        SenderId = message.SenderId,
        ReceiverId = message.ReceiverId,
        Content = message.Content,
        Timestamp = message.Timestamp
    };
}
