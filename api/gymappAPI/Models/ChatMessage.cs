namespace gymappAPI.Models;

public class ChatMessage
{
    public Guid Id { get; set; }
    public Guid SenderId { get; set; }
    public Guid ReceiverId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public User Sender { get; set; } = null!;
    public User Receiver { get; set; } = null!;
}