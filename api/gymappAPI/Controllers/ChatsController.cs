using gymappAPI.DTOs;
using gymappAPI.Managers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace gymappAPI.Controllers;

// Chat history. New messages are sent through the SignalR hub at /chatHub.
[ApiController]
[Authorize]
[Route("api/chats")]
public class ChatsController : ControllerBase
{
    private readonly IChatManager _chatManager;
    private readonly IUserManager _userManager;

    public ChatsController(IChatManager chatManager, IUserManager userManager)
    {
        _chatManager = chatManager;
        _userManager = userManager;
    }

    // GET api/chats/{otherUserId}/messages?before=...&limit=10
    [HttpGet("{otherUserId:guid}/messages")]
    public async Task<ActionResult<ChatHistoryDto>> GetMessages(Guid otherUserId, [FromQuery] DateTime? before,
        [FromQuery] int limit = 10)
    {
        if (limit is <= 0 or > 100)
            return BadRequest(new { message = "Limit must be between 1 and 100." });

        var user = await _userManager.GetCurrentUserAsync();
        return await _chatManager.GetHistoryAsync(user.Id, otherUserId, before, limit);
    }
}
