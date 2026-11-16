using gymappAPI.Data;
using gymappAPI.DTOs;
using gymappAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace gymappAPI.Managers;

public class ChatManager : IChatManager
{
    private readonly AppDbContext _context;

    public ChatManager(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ChatHistoryDto> GetHistoryAsync(Guid userId, Guid otherUserId, DateTime? before, int limit)
    {
        var query = _context.ChatMessages.Where(m =>
            (m.SenderId == userId && m.ReceiverId == otherUserId) ||
            (m.SenderId == otherUserId && m.ReceiverId == userId));

        if (before.HasValue)
            query = query.Where(m => m.Timestamp < before.Value);

        var messages = await query
            .OrderByDescending(m => m.Timestamp)
            .Take(limit)
            .ToListAsync();

        messages.Reverse();
        return new ChatHistoryDto
        {
            Messages = messages.Select(ChatMessageDto.From).ToArray(),
            HasMore = messages.Count == limit
        };
    }

    public async Task<ChatMessageDto?> SendMessageAsync(Guid senderId, Guid receiverId, string content)
    {
        var areFriends = await _context.FriendRequests.AnyAsync(fr => fr.Accepted &&
            ((fr.SenderId == senderId && fr.ReceiverId == receiverId) ||
             (fr.SenderId == receiverId && fr.ReceiverId == senderId)));
        if (!areFriends)
            return null;

        var message = new ChatMessage
        {
            SenderId = senderId,
            ReceiverId = receiverId,
            Content = content,
            Timestamp = DateTime.UtcNow
        };

        _context.ChatMessages.Add(message);
        await _context.SaveChangesAsync();
        return ChatMessageDto.From(message);
    }
}
