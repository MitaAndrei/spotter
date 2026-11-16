using gymappAPI.Managers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace gymappAPI.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private const int MaxMessageLength = 2000;

    private readonly IChatManager _chatManager;

    public ChatHub(IChatManager chatManager)
    {
        _chatManager = chatManager;
    }

    public async Task SendMessage(Guid receiverId, string message)
    {
        var senderId = Guid.Parse(Context.UserIdentifier!);

        message = message?.Trim() ?? string.Empty;
        if (message.Length == 0 || message.Length > MaxMessageLength)
            throw new HubException($"Message must be between 1 and {MaxMessageLength} characters.");

        var dto = await _chatManager.SendMessageAsync(senderId, receiverId, message)
                  ?? throw new HubException("You can only message your friends.");

        await Clients.User(receiverId.ToString()).SendAsync("ReceiveMessage", dto);
        await Clients.Caller.SendAsync("ReceiveMessage", dto);
    }
}
