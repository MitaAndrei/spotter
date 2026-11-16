using gymappAPI.DTOs;

namespace gymappAPI.Managers;

// Takes user ids explicitly because it is also called from the SignalR hub, outside an HTTP request.
public interface IChatManager
{
    // Up to `limit` messages between the two users sent before `before` (or the newest), oldest first.
    Task<ChatHistoryDto> GetHistoryAsync(Guid userId, Guid otherUserId, DateTime? before, int limit);

    // Saves the message, or returns null if the two users aren't friends.
    Task<ChatMessageDto?> SendMessageAsync(Guid senderId, Guid receiverId, string content);
}
