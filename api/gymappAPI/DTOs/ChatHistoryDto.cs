namespace gymappAPI.DTOs;

public class ChatHistoryDto
{
    public ChatMessageDto[] Messages { get; set; } = [];
    public bool HasMore { get; set; }
}
