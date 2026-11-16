using gymappAPI.DTOs;
using gymappAPI.Managers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace gymappAPI.Controllers;

// Friendships of the current user. {userId} is always the other person.
[ApiController]
[Authorize]
[Route("api/friends")]
public class FriendsController : ControllerBase
{
    private readonly IFriendsManager _friendsManager;

    public FriendsController(IFriendsManager friendsManager)
    {
        _friendsManager = friendsManager;
    }

    [HttpPost("requests")]
    public async Task<IActionResult> SendRequest([FromBody] FriendRequestDto request)
    {
        return await _friendsManager.SendRequestAsync(request.ReceiverId)
            ? NoContent()
            : Conflict(new { message = "A request already exists, or the user is invalid." });
    }

    [HttpPost("requests/{userId:guid}/accept")]
    public async Task<IActionResult> AcceptRequest(Guid userId)
    {
        return await _friendsManager.AcceptRequestAsync(userId) ? NoContent() : NotFound();
    }

    // Declines a request from userId.
    [HttpDelete("requests/{userId:guid}")]
    public async Task<IActionResult> RejectRequest(Guid userId)
    {
        return await _friendsManager.RejectRequestAsync(userId) ? NoContent() : NotFound();
    }

    [HttpDelete("{userId:guid}")]
    public async Task<IActionResult> Unfriend(Guid userId)
    {
        return await _friendsManager.UnfriendAsync(userId) ? NoContent() : NotFound();
    }

    [HttpGet("{userId:guid}/status")]
    public async Task<FriendshipStatus> GetStatus(Guid userId)
    {
        return await _friendsManager.GetFriendshipStatusAsync(userId);
    }
}
